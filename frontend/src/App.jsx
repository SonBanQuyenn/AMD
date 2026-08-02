import { Routes, Route, Link } from "react-router-dom";
import CreatePollPage from "./pages/CreatePollPage.jsx";
import VotePage from "./pages/VotePage.jsx";
import ResultsPage from "./pages/ResultsPage.jsx";

export default function App() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <Link to="/" className="home-link">
          ← Tạo poll mới
        </Link>
        <p className="app-tagline">đếm phiếu trực tiếp, không cần refresh</p>
      </header>

      <main className="app-main">
        <Routes>
          <Route path="/" element={<CreatePollPage />} />
          <Route path="/poll/:code" element={<VotePage />} />
          <Route path="/poll/:code/results" element={<ResultsPage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </div>
  );
}

function NotFound() {
  return (
    <div className="card">
      <p className="eyebrow">404</p>
      <h1>Không tìm thấy trang này</h1>
      <Link to="/" className="btn btn-primary">
        Về trang tạo poll
      </Link>
    </div>
  );
}
