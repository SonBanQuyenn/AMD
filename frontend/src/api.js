const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL;
const STORAGE_PREFIX = "pollapp";

async function request(path, options = {}) {
  const res = await fetch(`${GATEWAY_URL}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });

  if (!res.ok) {
    const message = await res.text().catch(() => "");
    const error = new Error(message || `Request failed (${res.status})`);
    error.status = res.status;
    throw error;
  }

  // 204/empty body responses (e.g. broadcast) - guard against JSON parse errors
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

export function createPoll({ question, options, expiresAt }) {
  return request("/polls", {
    method: "POST",
    body: JSON.stringify({ question, options, expiresAt: expiresAt || null }),
  });
}

// creatorToken is optional. Pass it in to also learn whether the caller is the
// poll's creator (poll.isCreator) - needed to gate the analyst/results page.
export function getPoll(code, creatorToken) {
  const query = creatorToken ? `?creatorToken=${encodeURIComponent(creatorToken)}` : "";
  return request(`/polls/${code}${query}`);
}

// Stop accepting votes early. Only succeeds if creatorToken matches the poll's owner.
export function closePoll(code, creatorToken) {
  return request(`/polls/${code}/close`, {
    method: "POST",
    body: JSON.stringify({ creatorToken }),
  });
}

export function castVote({ pollCode, optionIndex, voterToken }) {
  return request("/votes", {
    method: "POST",
    body: JSON.stringify({ pollCode, optionIndex, voterToken }),
  });
}

export function getResults(code) {
  return request(`/votes/${code}/results`);
}

// Một trình duyệt = một "voter identity" duy nhất, dùng lại cho mọi poll.
// Backend chặn vote trùng theo cặp (PollCode, VoterToken), nên dùng chung 1
// token toàn cục vẫn đúng - chỉ chặn vote 2 lần trên CÙNG 1 poll.
export function getVoterToken() {
  const key = `${STORAGE_PREFIX}:voterToken`;
  let token = localStorage.getItem(key);
  if (!token) {
    token = crypto.randomUUID();
    localStorage.setItem(key, token);
  }
  return token;
}

export function hasVotedOn(code) {
  return localStorage.getItem(`${STORAGE_PREFIX}:voted:${code}`) === "true";
}

export function markVotedOn(code) {
  localStorage.setItem(`${STORAGE_PREFIX}:voted:${code}`, "true");
}

// Creator token: sinh 1 lần lúc tạo poll (POST /polls trả về), lưu lại ở đây để
// những lần sau chứng minh "tôi là người tạo poll này" mà không cần đăng nhập.
// Chỉ trình duyệt đã tạo poll mới có token này -> chỉ trình duyệt đó xem được
// trang phân tích và dừng được poll trước hạn.
export function saveCreatorToken(code, creatorToken) {
  localStorage.setItem(`${STORAGE_PREFIX}:creator:${code}`, creatorToken);
}

export function getCreatorToken(code) {
  return localStorage.getItem(`${STORAGE_PREFIX}:creator:${code}`);
}
