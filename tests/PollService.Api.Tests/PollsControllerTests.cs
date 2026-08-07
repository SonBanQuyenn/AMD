using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollService.Api;
using PollService.Api.Controllers;
using Shared.Contracts.Dtos;
using Xunit;

namespace PollService.Api.Tests;

public class PollsControllerTests
{
    // Each test gets its own isolated in-memory database, so tests never
    // interfere with each other even when run in parallel.
    private static PollDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PollDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedPollWithCreatorToken()
    {
        await using var db = CreateDb();
        var controller = new PollsController(db);
        var request = new CreatePollRequest("Favorite color?", new List<string> { "Red", "Blue" }, null);

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var body = Assert.IsType<CreatePollResponse>(created.Value);
        Assert.Equal("Favorite color?", body.Question);
        Assert.Equal(2, body.Options.Count);
        Assert.False(string.IsNullOrWhiteSpace(body.CreatorToken));
        Assert.Equal(6, body.Code.Length);
        Assert.Single(db.Polls);
    }

    [Fact]
    public async Task Create_WithEmptyQuestion_ReturnsBadRequest()
    {
        await using var db = CreateDb();
        var controller = new PollsController(db);
        var request = new CreatePollRequest("   ", new List<string> { "A", "B" }, null);

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Polls);
    }

    [Fact]
    public async Task Create_WithOnlyOneOption_ReturnsBadRequest()
    {
        await using var db = CreateDb();
        var controller = new PollsController(db);
        var request = new CreatePollRequest("Valid question?", new List<string> { "OnlyOne" }, null);

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithMoreThanSixOptions_ReturnsBadRequest()
    {
        await using var db = CreateDb();
        var controller = new PollsController(db);
        var sevenOptions = Enumerable.Range(1, 7).Select(i => $"Option {i}").ToList();
        var request = new CreatePollRequest("Valid question?", sevenOptions, null);

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetStatus_ForUnknownCode_ReturnsExistsFalse()
    {
        await using var db = CreateDb();
        var controller = new PollsController(db);

        var result = await controller.GetStatus("does-not-exist");

        var ok = Assert.IsType<OkObjectResult>(result);
        var status = Assert.IsType<PollStatusResponse>(ok.Value);
        Assert.False(status.Exists);
        Assert.False(status.IsOpen);
    }

    [Fact]
    public async Task GetStatus_ForOpenPoll_ReturnsIsOpenTrueWithOptionCount()
    {
        await using var db = CreateDb();
        db.Polls.Add(new Poll
        {
            Id = Guid.NewGuid(),
            Code = "OPEN01",
            Question = "Q?",
            Options = new[] { "A", "B", "C" },
            IsClosed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null,
            CreatorToken = "secret"
        });
        await db.SaveChangesAsync();
        var controller = new PollsController(db);

        var result = await controller.GetStatus("OPEN01");

        var status = Assert.IsType<PollStatusResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(status.Exists);
        Assert.True(status.IsOpen);
        Assert.Equal(3, status.OptionCount);
    }

    [Fact]
    public async Task GetStatus_ForManuallyClosedPoll_ReturnsIsOpenFalse()
    {
        await using var db = CreateDb();
        db.Polls.Add(new Poll
        {
            Id = Guid.NewGuid(),
            Code = "CLOSED1",
            Question = "Q?",
            Options = new[] { "A", "B" },
            IsClosed = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null,
            CreatorToken = "secret"
        });
        await db.SaveChangesAsync();
        var controller = new PollsController(db);

        var result = await controller.GetStatus("CLOSED1");

        var status = Assert.IsType<PollStatusResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.False(status.IsOpen);
    }

    [Fact]
    public async Task GetStatus_ForExpiredPoll_ReturnsIsOpenFalse()
    {
        await using var db = CreateDb();
        db.Polls.Add(new Poll
        {
            Id = Guid.NewGuid(),
            Code = "EXPIRED",
            Question = "Q?",
            Options = new[] { "A", "B" },
            IsClosed = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // expired yesterday
            CreatorToken = "secret"
        });
        await db.SaveChangesAsync();
        var controller = new PollsController(db);

        var result = await controller.GetStatus("EXPIRED");

        var status = Assert.IsType<PollStatusResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.False(status.IsOpen);
    }

    [Fact]
    public async Task Close_WithWrongCreatorToken_ReturnsForbidden()
    {
        await using var db = CreateDb();
        db.Polls.Add(new Poll
        {
            Id = Guid.NewGuid(),
            Code = "SECURE1",
            Question = "Q?",
            Options = new[] { "A", "B" },
            IsClosed = false,
            CreatedAt = DateTime.UtcNow,
            CreatorToken = "correct-token"
        });
        await db.SaveChangesAsync();
        var controller = new PollsController(db);

        var result = await controller.Close("SECURE1", new ClosePollRequest("wrong-token"));

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);

        var poll = await db.Polls.FirstAsync(p => p.Code == "SECURE1");
        Assert.False(poll.IsClosed); // must not have been closed
    }

    [Fact]
    public async Task Close_WithCorrectCreatorToken_ClosesPoll()
    {
        await using var db = CreateDb();
        db.Polls.Add(new Poll
        {
            Id = Guid.NewGuid(),
            Code = "SECURE2",
            Question = "Q?",
            Options = new[] { "A", "B" },
            IsClosed = false,
            CreatedAt = DateTime.UtcNow,
            CreatorToken = "correct-token"
        });
        await db.SaveChangesAsync();
        var controller = new PollsController(db);

        var result = await controller.Close("SECURE2", new ClosePollRequest("correct-token"));

        Assert.IsType<OkObjectResult>(result);
        var poll = await db.Polls.FirstAsync(p => p.Code == "SECURE2");
        Assert.True(poll.IsClosed);
    }
}
