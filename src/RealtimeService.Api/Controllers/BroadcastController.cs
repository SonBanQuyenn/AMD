using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace RealtimeService.Api.Controllers;

[ApiController]
[Route("broadcast")]
public class BroadcastController(IHubContext<ResultsHub> hubContext) : ControllerBase
{
    // Vote service gọi endpoint này (best-effort, xem RealtimeServiceClient.cs bên Vote
    // service) mỗi khi có vote mới, kèm theo mảng số vote hiện tại của từng option.
    [HttpPost("{code}")]
    public async Task<IActionResult> Broadcast(string code, [FromBody] int[] counts)
    {
        await hubContext.Clients.Group(code).SendAsync("ResultsUpdated", counts);
        return Ok();
    }
}
