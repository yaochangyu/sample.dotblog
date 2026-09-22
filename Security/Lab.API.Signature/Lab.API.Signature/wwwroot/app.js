// Lab.API.Signature Client Demo
// ⚠️ 教學展示用：正式環境的 Secret 絕對不可以出現在瀏覽器 JS 裡，簽章計算必須在合作夥伴後端完成。
//
// Canonical String 規格（必須跟 Lab.API.Signature/Services/SignatureValidationHandler.cs 的
// BuildCanonicalString 完全一致）：
//   METHOD(大寫)
//   PATH
//   TIMESTAMP (Unix epoch seconds)
//   NONCE
//   API_KEY
//   BODY_SHA256_HEX (raw body bytes 的 SHA-256，小寫 hex；空 body 用空字串的 SHA-256)
// 六個欄位用 "\n" 換行分隔。

const DEMO_CREDENTIALS = {
  1: { apiKey: "demo-api-key-001", secret: "demo-secret-001-please-change" },
  2: { apiKey: "demo-api-key-002", secret: "demo-secret-002-please-change" },
  3: { apiKey: "demo-api-key-003", secret: "demo-secret-003-please-change" },
};

const GET_PATH = "/api/protected/orders/1";
const POST_PATH = "/api/protected/orders";

/** 記住上一次「正常請求」的完整資訊，供攻擊模擬按鈕沿用。 */
let lastRequest = null;

function bytesToHex(buffer) {
  return Array.from(new Uint8Array(buffer))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
}

async function sha256Hex(bodyText) {
  const bytes = new TextEncoder().encode(bodyText ?? "");
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  return bytesToHex(digest);
}

function buildCanonicalString(method, path, timestamp, nonce, apiKey, bodyHashHex) {
  return [method.toUpperCase(), path, timestamp, nonce, apiKey, bodyHashHex].join("\n");
}

async function hmacSha256Hex(secret, canonicalString) {
  const keyBytes = new TextEncoder().encode(secret);
  const messageBytes = new TextEncoder().encode(canonicalString);
  const cryptoKey = await crypto.subtle.importKey(
    "raw",
    keyBytes,
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"]
  );
  const signatureBuffer = await crypto.subtle.sign("HMAC", cryptoKey, messageBytes);
  return bytesToHex(signatureBuffer);
}

/** 組出完整簽章 Header 需要的所有欄位（不含 apiKey/secret，呼叫端自帶）。 */
async function buildSignedRequest({ method, path, bodyText, apiKey, secret, timestampOverride, nonceOverride }) {
  const timestamp = timestampOverride ?? Math.floor(Date.now() / 1000).toString();
  const nonce = nonceOverride ?? crypto.randomUUID();
  const bodyHashHex = await sha256Hex(method === "POST" ? bodyText : "");
  const canonicalString = buildCanonicalString(method, path, timestamp, nonce, apiKey, bodyHashHex);
  const signature = await hmacSha256Hex(secret, canonicalString);

  return { method, path, bodyText, apiKey, timestamp, nonce, signature };
}

async function sendRequest(requestInfo) {
  const { method, path, bodyText, apiKey, timestamp, nonce, signature } = requestInfo;

  const headers = {
    "X-Api-Key": apiKey,
    "X-Timestamp": timestamp,
    "X-Nonce": nonce,
    "X-Signature": signature,
  };

  const fetchOptions = { method, headers };
  if (method === "POST") {
    headers["Content-Type"] = "application/json";
    fetchOptions.body = bodyText;
  }

  const response = await fetch(path, fetchOptions);
  let responseBodyText;
  try {
    responseBodyText = JSON.stringify(await response.json(), null, 2);
  } catch {
    responseBodyText = "(無法解析回應 Body)";
  }

  return { status: response.status, bodyText: responseBodyText };
}

function renderResult(status, bodyText) {
  const resultEl = document.getElementById("result");
  resultEl.textContent = `HTTP ${status}\n\n${bodyText}`;
  resultEl.classList.remove("status-2xx", "status-401");
  resultEl.classList.add(status === 401 ? "status-401" : "status-2xx");
}

function getFormValues() {
  return {
    apiKey: document.getElementById("apiKey").value.trim(),
    secret: document.getElementById("secret").value.trim(),
    method: document.getElementById("method").value,
    bodyText: document.getElementById("body").value,
  };
}

async function handleSubmit() {
  const { apiKey, secret, method, bodyText } = getFormValues();
  const path = method === "GET" ? GET_PATH : POST_PATH;

  const requestInfo = await buildSignedRequest({ method, path, bodyText, apiKey, secret });
  lastRequest = { ...requestInfo, secret };

  const { status, bodyText: responseBodyText } = await sendRequest(requestInfo);
  renderResult(status, responseBodyText);
}

/** 攻擊 1：竄改 Body，但沿用上一次請求的舊簽章（模擬 Middleware 應偵測 Body Hash 不符）。 */
async function handleAttackTamperBody() {
  if (!lastRequest) {
    renderResult(0, "請先成功送出一次正常請求。");
    return;
  }

  const tamperedRequest = {
    ...lastRequest,
    bodyText: JSON.stringify({ productName: "被竄改的商品", amount: 999999 }),
    // 簽章刻意不重新計算，沿用上一次（針對原始 Body 算出的）簽章
  };

  const { status, bodyText } = await sendRequest(tamperedRequest);
  renderResult(status, bodyText);
}

/** 攻擊 2：完全重送上一次成功的請求（同一個 Nonce），模擬 Replay 攻擊。 */
async function handleAttackReplayNonce() {
  if (!lastRequest) {
    renderResult(0, "請先成功送出一次正常請求。");
    return;
  }

  const { status, bodyText } = await sendRequest(lastRequest);
  renderResult(status, bodyText);
}

/** 攻擊 3：使用 6 分鐘前的 Timestamp（超出 ±5 分鐘視窗），但簽章對該過期 timestamp 是正確的。 */
async function handleAttackExpiredTimestamp() {
  const { apiKey, secret, method, bodyText } = getFormValues();
  const path = method === "GET" ? GET_PATH : POST_PATH;
  const expiredTimestamp = (Math.floor(Date.now() / 1000) - 6 * 60).toString();

  const requestInfo = await buildSignedRequest({
    method,
    path,
    bodyText,
    apiKey,
    secret,
    timestampOverride: expiredTimestamp,
  });

  const { status, bodyText: responseBodyText } = await sendRequest(requestInfo);
  renderResult(status, responseBodyText);
}

function fillDemoCredentials(demoIndex) {
  const demo = DEMO_CREDENTIALS[demoIndex];
  document.getElementById("apiKey").value = demo.apiKey;
  document.getElementById("secret").value = demo.secret;
}

document.getElementById("submitBtn").addEventListener("click", () => {
  handleSubmit().catch((err) => renderResult(0, `發生錯誤：${err.message}`));
});
document.getElementById("attackTamperBody").addEventListener("click", () => {
  handleAttackTamperBody().catch((err) => renderResult(0, `發生錯誤：${err.message}`));
});
document.getElementById("attackReplayNonce").addEventListener("click", () => {
  handleAttackReplayNonce().catch((err) => renderResult(0, `發生錯誤：${err.message}`));
});
document.getElementById("attackExpiredTimestamp").addEventListener("click", () => {
  handleAttackExpiredTimestamp().catch((err) => renderResult(0, `發生錯誤：${err.message}`));
});
document.querySelectorAll(".quick-fill button").forEach((btn) => {
  btn.addEventListener("click", () => fillDemoCredentials(btn.dataset.demo));
});
