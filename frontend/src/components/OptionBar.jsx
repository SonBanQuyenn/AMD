import TallyMarks from "./TallyMarks.jsx";

export default function OptionBar({ label, count, total, isLeading }) {
  const percent = total > 0 ? Math.round((count / total) * 100) : 0;

  return (
    <div className={`option-bar ${isLeading ? "is-leading" : ""}`}>
      <div className="option-bar-fill" style={{ width: `${percent}%` }} aria-hidden="true" />
      <div className="option-bar-content">
        <div className="option-bar-top">
          <span className="option-bar-label">{label}</span>
          <span className="option-bar-count">{count}</span>
        </div>
        <div className="option-bar-bottom">
          <TallyMarks count={count} />
          <span className="option-bar-percent">{percent}%</span>
        </div>
      </div>
    </div>
  );
}
