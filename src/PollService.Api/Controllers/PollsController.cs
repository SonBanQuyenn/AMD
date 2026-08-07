using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Dtos;

namespace PollService.Api.Controllers;

[ApiController]
[Route("polls")]
public class PollsController(PollDbContext db) : ControllerBase
{
    // POST /polls - create a new poll
    [HttpPost]
    public async Task<IActionResult> Create(CreatePollRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        if (request.Options is null || request.Options.Count < 2 || request.Options.Count > 6)
            return BadRequest("A poll needs between 2 and 6 options.");

        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Code = ShortCodeGenerator.Generate(),
            Question = request.Question.Trim(),
            Options = request.Options.Select(o => o.Trim()).ToArray(),
            IsClosed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
            CreatorToken = Guid.NewGuid().ToString("N")
        };

        db.Polls.Add(poll);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { code = poll.Code }, new CreatePollResponse(
            poll.Code,
            poll.Question,
            poll.Options.ToList(),
            poll.IsClosed,
            poll.CreatedAt,
            poll.ExpiresAt,
            poll.CreatorToken));
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code, [FromQuery] string? creatorToken)
    {
        var poll = await db.Polls.FirstOrDefaultAsync(p => p.Code == code);
        if (poll is null) return NotFound();

        var isCreator = !string.IsNullOrEmpty(creatorToken) && creatorToken == poll.CreatorToken;
        return Ok(ToResponse(poll, isCreator));
    }

    // GET /polls/{code}/status - lightweight endpoint for OTHER SERVICES to call.
    // This is the service-to-service contract: Vote service calls this before
    // accepting a vote, instead of duplicating poll validation logic.
    [HttpGet("{code}/status")]
    public async Task<IActionResult> GetStatus(string code)
    {
        var poll = await db.Polls.FirstOrDefaultAsync(p => p.Code == code);

        if (poll is null)
            return Ok(new PollStatusResponse(Exists: false, IsOpen: false, OptionCount: 0));

        var isExpired = poll.ExpiresAt is not null && poll.ExpiresAt < DateTime.UtcNow;
        var isOpen = !poll.IsClosed && !isExpired;

        return Ok(new PollStatusResponse(Exists: true, IsOpen: isOpen, OptionCount: poll.Options.Length));
    }

    // POST /polls/{code}/close - only the creator can stop accepting new votes early
    // (also used to manually stop a poll that hasn't reached its ExpiresAt yet).
    [HttpPost("{code}/close")]
    public async Task<IActionResult> Close(string code, ClosePollRequest request)
    {
        var poll = await db.Polls.FirstOrDefaultAsync(p => p.Code == code);
        if (poll is null) return NotFound();

        if (string.IsNullOrEmpty(request.CreatorToken) || request.CreatorToken != poll.CreatorToken)
            return StatusCode(403, "Only the poll creator can close this poll.");

        poll.IsClosed = true;
        await db.SaveChangesAsync();

        return Ok(ToResponse(poll, isCreator: true));
    }

    private static PollResponse ToResponse(Poll poll, bool isCreator) => new(
        poll.Code,
        poll.Question,
        poll.Options.ToList(),
        poll.IsClosed,
        poll.CreatedAt,
        poll.ExpiresAt,
        IsExpired: poll.ExpiresAt is not null && poll.ExpiresAt < DateTime.UtcNow,
        IsCreator: isCreator);
}
