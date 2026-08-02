import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { getPoll, castVote, getVoterToken, hasVotedOn, markVotedOn, getCreatorToken } from "../api.js";

export default function VotePage() {
  const { code } = useParams();
  const [poll, setPoll] = useState(null);
  const [loadError, setLoadError] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [voteError, setVoteError] = useState(null);
  const [voted, setVoted] = useState(() => hasVotedOn(code));
  // Trang phân tích chỉ dành cho người tạo poll - chỉ hiện link đó nếu chính
  // trình duyệt này đã tạo ra poll (có creatorToken lưu sẵn từ lúc tạo).
  const isCreator = Boolean(getCreatorToken(code));

  useEffect(() => {
    let cancelled = false;

    getPoll(code)
      .then((data) => {
        if (!cancelled) setPoll(data);
      })
      .catch((err) => {
        if (!cancelled) setLoadError(err.status === 404 ? "Không tìm thấy poll này." : err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [code]);

  async function handleVote(optionIndex) {
    setVoteError(null);
    setSubmitting(true);
    try {
      await castVote({ pollCode: code, optionIndex, voterToken: getVoterToken() });
      markVotedOn(code);
      setVoted(true);
    } catch (err) {
      if (err.status === 409) {
        markVotedOn(code);
        setVoted(true);
      } else if (err.status === 400) {
        setVoteError(
          poll?.isExpired ? "Poll này đã hết hạn, không nhận thêm phiếu." : "Poll này đã đóng, không nhận thêm phiếu."
        );
      } else {
        setVoteError(err.message || "Không gửi được phiếu, thử lại.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (loadError) {
    return (
      <div className="card">
        <p className="eyebrow">Lỗi</p>
        <h1>{loadError}</h1>
        <Link to="/" className="btn btn-primary">
          Tạo poll mới
        </Link>
      </div>
    );
  }

  if (!poll) {
    return (
      <div className="card">
        <p className="eyebrow">Đang tải</p>
        <h1>Đang mở poll...</h1>
      </div>
    );
  }

  if (voted) {
    return (
      <div className="card">
        <p className="eyebrow">Cảm ơn bạn</p>
        <h1>Phiếu của bạn đã được ghi nhận.</h1>
        {isCreator && (
          <Link to={`/poll/${code}/results`} className="btn btn-primary">
            Xem trang phân tích
          </Link>
        )}
      </div>
    );
  }

  return (
    <div className="card">
      <p className="eyebrow">Đang mở phiếu</p>
      <h1>{poll.question}</h1>

      <div className="vote-options">
        {poll.options.map((label, index) => (
          <button
            key={index}
            className="vote-option"
            onClick={() => handleVote(index)}
            disabled={submitting || poll.isClosed || poll.isExpired}
          >
            <span className="vote-option-index">{String.fromCharCode(65 + index)}</span>
            <span className="vote-option-label">{label}</span>
          </button>
        ))}
      </div>

      {poll.isExpired && <p className="form-error">Poll này đã hết hạn, không nhận thêm phiếu.</p>}
      {!poll.isExpired && poll.isClosed && <p className="form-error">Poll này đã đóng.</p>}
      {voteError && <p className="form-error">{voteError}</p>}

      {isCreator && (
        <Link to={`/poll/${code}/results`} className="link-muted">
          Xem trang phân tích →
        </Link>
      )}
    </div>
  );
}
