using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Dtos;

namespace VoteService.Api.Controllers;

[ApiController]
[Route("votes")]
public class VotesController(
    VoteDbContext db,
    PollServiceClient pollService,
    RealtimeServiceClient realtimeService) : ControllerBase
{
    // POST /votes - cast a vote. This is where the service-to-service call happens.
    [HttpPost]
    public async Task<IActionResult> CastVote(CastVoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PollCode) || string.IsNullOrWhiteSpace(request.VoterToken))
            return BadRequest("PollCode and VoterToken are required.");

        // 1. Ask Poll service whether this poll exists, is open, and how many options it has.
        //    Vote service deliberately has zero business logic about polls themselves.
        PollStatusResponse status;
        try
        {
            status = await pollService.GetStatusAsync(request.PollCode);
        }
        catch (HttpRequestException)
        {
            return Problem("Poll service is unavailable, please try again shortly.", statusCode: 503);
        }

        if (!status.Exists)
            return NotFound("Poll not found.");

        if (!status.IsOpen)
            return BadRequest("This poll is closed.");

        if (request.OptionIndex < 0 || request.OptionIndex >= status.OptionCount)
            return BadRequest("Invalid option.");

        // 2. Prevent double voting (unique index on PollCode + VoterToken also enforces this at the DB level).
        var alreadyVoted = await db.Votes.AnyAsync(v =>
            v.PollCode == request.PollCode && v.VoterToken == request.VoterToken);

        if (alreadyVoted)
            return Conflict("You have already voted on this poll.");

        db.Votes.Add(new Vote
        {
            Id = Guid.NewGuid(),
            PollCode = request.PollCode,
            OptionIndex = request.OptionIndex,
            VoterToken = request.VoterToken,
            VotedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // 3. Recompute counts and notify Realtime service (best effort - see RealtimeServiceClient).
        var counts = await GetCountsAsync(request.PollCode, status.OptionCount);
        await realtimeService.NotifyResultsUpdatedAsync(request.PollCode, counts);

        return Ok(new VoteResultsResponse(request.PollCode, counts, counts.Sum()));
    }

    // GET /votes/{code}/results - current tally, used by the results page on first load
    // (before the SignalR connection takes over for live updates).
    [HttpGet("{code}/results")]
    public async Task<IActionResult> GetResults(string code)
    {
        PollStatusResponse status;
        try
        {
            status = await pollService.GetStatusAsync(code);
        }
        catch (HttpRequestException)
        {
            return Problem("Poll service is unavailable, please try again shortly.", statusCode: 503);
        }

        if (!status.Exists)
            return NotFound("Poll not found.");

        var counts = await GetCountsAsync(code, status.OptionCount);
        return Ok(new VoteResultsResponse(code, counts, counts.Sum()));
    }

    private async Task<int[]> GetCountsAsync(string pollCode, int optionCount)
    {
        var counts = new int[optionCount];

        var grouped = await db.Votes
            .Where(v => v.PollCode == pollCode)
            .GroupBy(v => v.OptionIndex)
            .Select(g => new { OptionIndex = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var g in grouped)
        {
            if (g.OptionIndex >= 0 && g.OptionIndex < optionCount)
                counts[g.OptionIndex] = g.Count;
        }

        return counts;
    }
}
