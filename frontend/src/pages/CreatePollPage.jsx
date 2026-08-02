import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createPoll, saveCreatorToken } from "../api.js";

const MIN_OPTIONS = 2;
const MAX_OPTIONS = 6;

export default function CreatePollPage() {
  const navigate = useNavigate();
  const [question, setQuestion] = useState("");
  const [options, setOptions] = useState(["", ""]);
  const [expiresAt, setExpiresAt] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  function updateOption(index, value) {
    setOptions((prev) => prev.map((o, i) => (i === index ? value : o)));
  }

  function addOption() {
    if (options.length >= MAX_OPTIONS) return;
    setOptions((prev) => [...prev, ""]);
  }

  function removeOption(index) {
    if (options.length <= MIN_OPTIONS) return;
    setOptions((prev) => prev.filter((_, i) => i !== index));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);

    const trimmedQuestion = question.trim();
    const trimmedOptions = options.map((o) => o.trim()).filter(Boolean);

    if (!trimmedQuestion) {
      setError("Cần nhập câu hỏi.");
      return;
    }
    if (trimmedOptions.length < MIN_OPTIONS) {
      setError(`Cần ít nhất ${MIN_OPTIONS} lựa chọn có nội dung.`);
      return;
    }

    // <input type="datetime-local"> trả về giờ local không có timezone
    // (vd "2026-08-01T20:30") - new Date(...) hiểu nó theo giờ local của trình
    // duyệt, .toISOString() thì đổi sang UTC để gửi lên backend (DateTime? ExpiresAt).
    let expiresAtIso = null;
    if (expiresAt) {
      const parsed = new Date(expiresAt);
      if (Number.isNaN(parsed.getTime())) {
        setError("Thời gian hết hạn không hợp lệ.");
        return;
      }
      if (parsed.getTime() <= Date.now()) {
        setError("Thời gian hết hạn phải ở trong tương lai.");
        return;
      }
      expiresAtIso = parsed.toISOString();
    }

    setLoading(true);
    try {
      const poll = await createPoll({
        question: trimmedQuestion,
        options: trimmedOptions,
        expiresAt: expiresAtIso,
      });

      // Chỉ trình duyệt này nhận được creatorToken (response của POST /polls) -
      // lưu lại để sau này chứng minh quyền chủ poll (xem trang phân tích, dừng poll).
      saveCreatorToken(poll.code, poll.creatorToken);

      // Người tạo poll mặc định được chuyển thẳng sang trang phân tích của poll đó.
      navigate(`/poll/${poll.code}/results`);
    } catch (err) {
      setError(err.message || "Không tạo được poll, thử lại sau.");
      setLoading(false);
    }
  }

  return (
    <div className="card">
      <p className="eyebrow">Tạo poll mới</p>
      <h1>Đặt một câu hỏi, xem phiếu về ngay khi có người trả lời.</h1>

      <form onSubmit={handleSubmit} className="poll-form">
        <label className="field">
          <span className="field-label">Câu hỏi</span>
          <textarea
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="Ngôn ngữ lập trình bạn thích nhất?"
            rows={2}
            maxLength={500}
          />
        </label>

        <div className="field-label">Lựa chọn (2–6)</div>
        <div className="option-list">
          {options.map((value, index) => (
            <div className="option-row" key={index}>
              <span className="option-index">{index + 1}</span>
              <input
                type="text"
                value={value}
                onChange={(e) => updateOption(index, e.target.value)}
                placeholder={`Lựa chọn ${index + 1}`}
                maxLength={100}
              />
              {options.length > MIN_OPTIONS && (
                <button
                  type="button"
                  className="option-remove"
                  onClick={() => removeOption(index)}
                  aria-label={`Xóa lựa chọn ${index + 1}`}
                >
                  ×
                </button>
              )}
            </div>
          ))}
        </div>

        {options.length < MAX_OPTIONS && (
          <button type="button" className="btn btn-ghost" onClick={addOption}>
            + Thêm lựa chọn
          </button>
        )}

        <label className="field">
          <span className="field-label">Thời gian hết hạn (tùy chọn)</span>
          <input
            type="datetime-local"
            value={expiresAt}
            onChange={(e) => setExpiresAt(e.target.value)}
          />
          <span className="field-hint">
            Sau thời điểm này, poll tự đóng và không ai vote được nữa. Để trống nếu
            không muốn giới hạn thời gian - bạn vẫn có thể tự dừng poll bất cứ lúc nào
            ở trang phân tích.
          </span>
        </label>

        {error && <p className="form-error">{error}</p>}

        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Đang tạo..." : "Tạo poll"}
        </button>
      </form>
    </div>
  );
}
