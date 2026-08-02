namespace VoteService.Api;

// Realtime service chưa được code (bước tiếp theo trong roadmap), nên client này
// gọi "best effort": nếu Realtime service chưa chạy hoặc lỗi, Vote service vẫn
// phải trả lời frontend thành công bình thường - không được để 1 service phụ
// (realtime) làm sập luồng vote chính.
public class RealtimeServiceClient(HttpClient httpClient, ILogger<RealtimeServiceClient> logger)
{
    public async Task NotifyResultsUpdatedAsync(string pollCode, int[] counts, CancellationToken ct = default)
    {
        try
        {
            await httpClient.PostAsJsonAsync($"/broadcast/{pollCode}", counts, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not reach realtime service to broadcast results for poll {PollCode}", pollCode);
        }
    }
}
