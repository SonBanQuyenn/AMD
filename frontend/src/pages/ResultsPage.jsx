import { useEffect, useRef, useState } from "react";
import { useParams, Link } from "react-router-dom";
import * as signalR from "@microsoft/signalr";
import { getPoll, getResults, closePoll, getCreatorToken } from "../api.js";
import OptionBar from "../components/OptionBar.jsx";
import LiveDot from "../components/LiveDot.jsx";

const REALTIME_URL = import.meta.env.VITE_REALTIME_URL;

// Trang phân tích (analyst) của poll. CHỈ người đã tạo poll (trình duyệt có
// creatorToken khớp, lưu ở localStorage lúc tạo) mới xem được trang này.
export default function ResultsPage() {
  const { code } = useParams();
  const [poll, setPoll] = useState(null);
  const [counts, setCounts] = useState(null);
  const [connected, setConnected] = useState(false);
  const [loadError, setLoadError] = useState(null);
  const [notCreator, setNotCreator] = useState(false);
  const [closing, setClosing] = useState(false);
  const [closeError, setCloseError] = useState(null);
  const connectionRef = useRef(null);

  const creatorToken = getCreatorToken(code);

  // Initial load: poll info (+ quyền chủ poll) + tally hiện tại, song song.
  useEffect(() => {
    let cancelled = false;

    Promise.all([getPoll(code, creatorToken), getResults(code)])
      .then(([pollData, resultsData]) => {
        if (cancelled) return;

        if (!pollData.isCreator) {
          setNotCreator(true);
          return;
        }

        setPoll(pollData);
        setCounts(resultsData.counts);
      })
      .catch((err) => {
        if (!cancelled) setLoadError(err.status === 404 ? "Không tìm thấy poll này." : err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [code, creatorToken]);

  // Live updates: connect straight to Realtime service's SignalR hub.
  // (Không qua gateway - xem ghi chú trong README về Ocelot + WebSocket.)
  useEffect(() => {
    if (notCreator || loadError) return undefined;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${REALTIME_URL}/hubs/results`, { withCredentials: false })
      .withAutomaticReconnect()
      .build();

    connection.on("ResultsUpdated", (newCounts) => {
      setCounts(newCounts);
    });

    connection.onreconnecting(() => setConnected(false));
    connection.onreconnected(() => {
      setConnected(true);
      connection.invoke("JoinPoll", code).catch(() => {});
    });
    connection.onclose(() => setConnected(false));

    connection
      .start()
      .then(() => {
        setConnected(true);
        return connection.invoke("JoinPoll", code);
      })
      .catch(() => setConnected(false));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [code, notCreator, loadError]);

  async function handleStopVoting() {
    setCloseError(null);
    setClosing(true);
    try {
      const updated = await closePoll(code, creatorToken);
      setPoll(updated);
    } catch (err) {
      setCloseError(err.message || "Không dừng được poll, thử lại.");
    } finally {
      setClosing(false);
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

  if (notCreator) {
    return (
      <div className="card">
        <p className="eyebrow">Không có quyền truy cập</p>
        <h1>Chỉ người tạo poll mới xem được trang phân tích này.</h1>
        <p className="app-tagline">
          Nếu bạn có link vote của poll này, hãy dùng link đó để bỏ phiếu thay vì xem
          kết quả chi tiết.
        </p>
        <Link to={`/poll/${code}`} className="btn btn-primary">
          Đến trang vote
        </Link>
      </div>
    );
  }

  if (!poll || !counts) {
    return (
      <div className="card">
        <p className="eyebrow">Đang tải</p>
        <h1>Đang lấy kết quả...</h1>
      </div>
    );
  }

  const total = counts.reduce((sum, c) => sum + c, 0);
  const maxCount = Math.max(...counts);
  const voteUrl = `${window.location.origin}/poll/${code}`;
  const votingOpen = !poll.isClosed && !poll.isExpired;

  return (
    <div className="card">
      <div className="results-header">
        <p className="eyebrow">Trang phân tích (chỉ bạn thấy)</p>
        <LiveDot connected={connected} />
      </div>
      <h1>{poll.question}</h1>

      <div className="share-links">
        <div className="share-link">
          <div>
            <span className="share-link-label">Link để chia sẻ cho người vote</span>
            <code className="share-link-url">{voteUrl}</code>
          </div>
          <button
            className="btn btn-ghost btn-small"
            onClick={() => navigator.clipboard?.writeText(voteUrl).catch(() => {})}
          >
            Copy
          </button>
        </div>
      </div>

      <PollStatusBanner poll={poll} />

      <p className="results-total">
        <span className="mono">{total}</span> phiếu đã ghi nhận
      </p>

      <div className="option-bar-list">
        {poll.options.map((label, index) => (
          <OptionBar
            key={index}
            label={label}
            count={counts[index] ?? 0}
            total={total}
            isLeading={total > 0 && counts[index] === maxCount && maxCount > 0}
          />
        ))}
      </div>

      {votingOpen && (
        <div className="stop-voting">
          <button className="btn btn-ghost" onClick={handleStopVoting} disabled={closing}>
            {closing ? "Đang dừng..." : "Dừng nhận phiếu ngay"}
          </button>
          {closeError && <p className="form-error">{closeError}</p>}
        </div>
      )}
    </div>
  );
}

function PollStatusBanner({ poll }) {
  if (poll.isExpired) {
    return (
      <p className="form-error">
        Poll đã hết hạn lúc {new Date(poll.expiresAt).toLocaleString("vi-VN")} - không còn
        nhận phiếu.
      </p>
    );
  }
  if (poll.isClosed) {
    return <p className="form-error">Bạn đã dừng nhận phiếu cho poll này.</p>;
  }
  if (poll.expiresAt) {
    return (
      <p className="app-tagline">
        Đang mở phiếu - tự động hết hạn lúc {new Date(poll.expiresAt).toLocaleString("vi-VN")}.
      </p>
    );
  }
  return <p className="app-tagline">Đang mở phiếu - không giới hạn thời gian.</p>;
}
