using Microsoft.AspNetCore.SignalR;

namespace RealtimeService.Api;

public class ResultsHub : Hub
{
    // Frontend gọi hàm này ngay sau khi kết nối, để "đăng ký" nhận update
    // của đúng poll đang xem (mỗi poll code là 1 group riêng).
    public async Task JoinPoll(string pollCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, pollCode);
    }
}
