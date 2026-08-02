namespace VoteService.Api;

public class Vote
{
    public Guid Id { get; set; }

    // Not a foreign key - Vote service has NO direct access to Poll service's database.
    // It only knows the poll's code and trusts Poll service's /status endpoint.
    public string PollCode { get; set; } = default!;

    public int OptionIndex { get; set; }

    public string VoterToken { get; set; } = default!;

    public DateTime VotedAt { get; set; }
}
