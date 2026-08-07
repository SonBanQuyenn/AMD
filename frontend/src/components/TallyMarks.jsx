function TallyGroup({ count }) {
  const strokes = Math.min(count, 4);
  const positions = [4, 10, 16, 22];

  return (
    <svg width="30" height="24" viewBox="0 0 30 24" className="tally-group" aria-hidden="true">
      {positions.slice(0, strokes).map((x, i) => (
        <line key={i} x1={x} y1="3" x2={x} y2="21" className="tally-stroke" />
      ))}
      {count === 5 && <line x1="2" y1="21" x2="26" y2="3" className="tally-stroke tally-cross" />}
    </svg>
  );
}

const MAX_VISIBLE_GROUPS = 24;

export default function TallyMarks({ count }) {
  if (count === 0) {
    return <span className="tally-empty">chưa có phiếu</span>;
  }

  const fullGroups = Math.floor(count / 5);
  const remainder = count % 5;
  const groupCounts = [...Array(fullGroups).fill(5), ...(remainder > 0 ? [remainder] : [])];

  const visible = groupCounts.slice(0, MAX_VISIBLE_GROUPS);
  const hiddenGroupCount = groupCounts.length - visible.length;

  return (
    <div className="tally-marks" role="img" aria-label={`${count} phiếu`}>
      {visible.map((c, i) => (
        <TallyGroup key={i} count={c} />
      ))}
      {hiddenGroupCount > 0 && (
        <span className="tally-overflow">+{hiddenGroupCount * 5} nữa</span>
      )}
    </div>
  );
}
