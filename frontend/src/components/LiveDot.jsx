export default function LiveDot({ connected }) {
  return (
    <span className={`live-dot-wrap ${connected ? "is-live" : "is-offline"}`}>
      <span className="live-dot" />
      {connected ? "đang đếm trực tiếp" : "đang kết nối..."}
    </span>
  );
}
