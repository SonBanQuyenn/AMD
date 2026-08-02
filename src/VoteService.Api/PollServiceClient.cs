using Shared.Contracts.Dtos;

namespace VoteService.Api;

// Wraps the call to Poll service's GET /polls/{code}/status endpoint.
// Vote service NEVER touches Poll service's database directly - only this HTTP call.
public class PollServiceClient(HttpClient httpClient)
{
    public async Task<PollStatusResponse> GetStatusAsync(string pollCode, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/polls/{pollCode}/status", ct);
        response.EnsureSuccessStatusCode();

        var status = await response.Content.ReadFromJsonAsync<PollStatusResponse>(cancellationToken: ct);
        return status ?? new PollStatusResponse(Exists: false, IsOpen: false, OptionCount: 0);
    }
}
