using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts.Dtos;
using VoteService.Api;
using VoteService.Api.Controllers;
using Xunit;

namespace VoteService.Api.Tests;

public class VotesControllerTests
{
    private static VoteDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<VoteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Wraps a PollServiceClient whose HTTP call always returns the given status,
    // simulating whatever Poll Service would have replied with.
    private static PollServiceClient CreatePollServiceClient(PollStatusResponse status)
    {
        var handler = new FakeHttpMessageHandler(_ => TestHttp.JsonResponse(status));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://poll-service.test") };
        return new PollServiceClient(httpClient);
    }

    // reachable = true  -> Realtime Service responds normally (200 OK)
    // reachable = false -> every call throws, simulating Realtime Service being down
    private static RealtimeServiceClient CreateRealtimeServiceClient(bool reachable)
    {
        HttpMessageHandler handler = reachable
            ? new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))
            : new UnavailableHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://realtime-service.test") };
        return new RealtimeServiceClient(httpClient, NullLogger<RealtimeServiceClient>.Instance);
    }

    [Fact]
    public async Task CastVote_WhenPollDoesNotExist_ReturnsNotFound()
    {
        await using var db = CreateDb();
        var poll = CreatePollServiceClient(new PollStatusResponse(Exists: false, IsOpen: false, OptionCount: 0));
        var realtime = CreateRealtimeServiceClient(reachable: true);
        var controller = new VotesController(db, poll, realtime);

        var result = await controller.CastVote(new CastVoteRequest("MISSING", 0, "voter-1"));

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(db.Votes);
    }

    [Fact]
    public async Task CastVote_WhenPollIsClosed_ReturnsBadRequest()
    {
        await using var db = CreateDb();
        var poll = CreatePollServiceClient(new PollStatusResponse(Exists: true, IsOpen: false, OptionCount: 2));
        var realtime = CreateRealtimeServiceClient(reachable: true);
        var controller = new VotesController(db, poll, realtime);

        var result = await controller.CastVote(new CastVoteRequest("CLOSED1", 0, "voter-1"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Votes);
    }

    [Fact]
    public async Task CastVote_WhenOptionIndexOutOfRange_ReturnsBadRequest()
    {
        await using var db = CreateDb();
        var poll = CreatePollServiceClient(new PollStatusResponse(Exists: true, IsOpen: true, OptionCount: 2));
        var realtime = CreateRealtimeServiceClient(reachable: true);
        var controller = new VotesController(db, poll, realtime);

        var result = await controller.CastVote(new CastVoteRequest("OPEN01", 5, "voter-1"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Votes);
    }

    [Fact]
    public async Task CastVote_WhenVoterAlreadyVoted_ReturnsConflict()
    {
        await using var db = CreateDb();
        db.Votes.Add(new Vote
        {
            Id = Guid.NewGuid(),
            PollCode = "OPEN01",
            OptionIndex = 0,
            VoterToken = "voter-1",
            VotedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var poll = CreatePollServiceClient(new PollStatusResponse(Exists: true, IsOpen: true, OptionCount: 2));
        var realtime = CreateRealtimeServiceClient(reachable: true);
        var controller = new VotesController(db, poll, realtime);

        var result = await controller.CastVote(new CastVoteRequest("OPEN01", 1, "voter-1"));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Votes); // still just the original vote, not a second one
    }

    [Fact]
    public async Task CastVote_WhenValid_SavesVoteAndReturnsUpdatedTally()
    {
        await using var db = CreateDb();
        var poll = CreatePollServiceClient(new PollStatusResponse(Exists: true, IsOpen: true, OptionCount: 3));
        var realtime = CreateRealtimeServiceClient(reachable: true);
        var controller = new VotesController(db, poll, realtime);

        var result = await controller.CastVote(new CastVoteRequest("OPEN01", 1, "voter-1"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<VoteResultsResponse>(ok.Value);
        Assert.Equal(new[] { 0, 1, 0 }, body.Counts);
        Assert.Equal(1, body.TotalVotes);
        Assert.Single(db.Votes);
    }

    [Fact]
    public async Task CastVote_WhenRealtimeServiceIsUnreachable_VoteStillSucceeds()
    {
        // This is the resilience behaviour described in the individual report:
        // a broadcast failure must never fail the vote itself.
        await using var db = CreateDb();
        var poll = CreatePollServiceClient(new PollStatusResponse(Exists: true, IsOpen: true, OptionCount: 2));
        var realtime = CreateRealtimeServiceClient(reachable: false); // simulate Realtime Service being down
        var controller = new VotesController(db, poll, realtime);

        var result = await controller.CastVote(new CastVoteRequest("OPEN01", 0, "voter-1"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<VoteResultsResponse>(ok.Value);
        Assert.Equal(1, body.TotalVotes);
        Assert.Single(db.Votes); // the vote was still persisted despite the broadcast failing
    }

    [Fact]
    public async Task GetResults_ReturnsCorrectTallyAcrossOptions()
    {
        await using var db = CreateDb();
        db.Votes.AddRange(
            new Vote { Id = Guid.NewGuid(), PollCode = "OPEN01", OptionIndex = 0, VoterToken = "v1", VotedAt = DateTime.UtcNow },
            new Vote { Id = Guid.NewGuid(), PollCode = "OPEN01", OptionIndex = 1, VoterToken = "v2", VotedAt = DateTime.UtcNow },
            new Vote { Id = Guid.NewGuid(), PollCode = "OPEN01", OptionIndex = 1, VoterToken = "v3", VotedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var poll = CreatePollServiceClient(new PollStatusResponse(Exists: true, IsOpen: true, OptionCount: 2));
        var realtime = CreateRealtimeServiceClient(reachable: true);
        var controller = new VotesController(db, poll, realtime);

        var result = await controller.GetResults("OPEN01");

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<VoteResultsResponse>(ok.Value);
        Assert.Equal(new[] { 1, 2 }, body.Counts);
        Assert.Equal(3, body.TotalVotes);
    }
}
