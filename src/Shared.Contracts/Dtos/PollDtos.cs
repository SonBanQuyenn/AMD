namespace Shared.Contracts.Dtos;

// ---- Requests coming from the frontend ----

public record CreatePollRequest(string Question, List<string> Options, DateTime? ExpiresAt);

// CreatorToken: phải khớp với token bí mật sinh ra lúc tạo poll (xem CreatePollResponse)
// thì mới được phép đóng poll trước hạn hoặc xem trang phân tích.
public record ClosePollRequest(string CreatorToken);

// ---- Responses returned to the frontend ----

public record PollResponse(
    string Code,
    string Question,
    List<string> Options,
    bool IsClosed,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsExpired,
    bool IsCreator);

// Chỉ trả về DUY NHẤT 1 LẦN ngay sau khi tạo poll xong. CreatorToken không bao giờ
// được trả lại ở GET /polls/{code} - client (người tạo) phải tự lưu token này
// (ví dụ trong localStorage) để chứng minh quyền chủ poll ở những lần gọi sau.
public record CreatePollResponse(
    string Code,
    string Question,
    List<string> Options,
    bool IsClosed,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string CreatorToken);

// ---- Used ONLY for service-to-service calls (Vote service -> Poll service) ----
// This is the contract that proves the two services talk to each other.
// Keep it minimal: Vote service only needs to know "does this poll exist,
// is it still open, and how many options does it have" before accepting a vote.

public record PollStatusResponse(
    bool Exists,
    bool IsOpen,
    int OptionCount);

// ---- Vote service DTOs ----

// VoterToken: một chuỗi random được frontend tự sinh và lưu ở localStorage,
// gửi kèm mỗi lần vote. Cách đơn giản để chặn 1 người vote 2 lần mà không cần
// tài khoản/đăng nhập - đủ dùng cho scope bài này.
public record CastVoteRequest(string PollCode, int OptionIndex, string VoterToken);

public record VoteResultsResponse(string PollCode, int[] Counts, int TotalVotes);
