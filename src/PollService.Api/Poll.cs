namespace PollService.Api;

// This is the "own poll DB" entity from the architecture diagram.
// Only Poll service reads/writes this table directly.
public class Poll
{
    public Guid Id { get; set; }

    // Short shareable code, e.g. "7fGh2" -> used in the URL /poll/7fGh2
    public string Code { get; set; } = default!;

    public string Question { get; set; } = default!;

    // Stored as JSON in the DB (SQL Server has no native array type) via the
    // value converter configured in PollDbContext.OnModelCreating.
    public string[] Options { get; set; } = Array.Empty<string>();

    public bool IsClosed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    // Bí mật sinh ra lúc tạo poll, chỉ trả về cho client MỘT LẦN DUY NHẤT trong
    // response của POST /polls. Frontend lưu token này lại (localStorage) và gửi
    // kèm ở các lần gọi sau để chứng minh "tôi là người tạo poll này" - dùng để
    // chặn quyền xem trang phân tích (analyst) và quyền dừng poll trước hạn.
    // Không cần bảng User/đăng nhập vì scope bài này không yêu cầu tài khoản.
    public string CreatorToken { get; set; } = default!;
}
