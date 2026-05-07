const AUTH_SERVER = window.location.origin;
const DEMO_CLIENT_ID = 'spa-lab';
const REDIRECT_URI = `${window.location.origin}/emulator.html`;
const cspEnabled = new URLSearchParams(window.location.search).get('csp') !== 'off';
const state = {
    verifier: '',
    challenge: '',
    authCode: '',
    token: '',
    loggedInUser: '',
    lastCspViolation: null
};

// [Lab] 刻意暴露至 window，供 XSS 攻擊模擬讀取；正式環境不應將 state 掛到 window
window.__labState = state;

const UI = {
    log: (msg, type = '') => {
        const el = document.getElementById('consoleLog');
        const div = document.createElement('div');
        div.textContent = `[${new Date().toLocaleTimeString('zh-TW', { hour12: false })}] ${msg}`;
        if (type) div.className = `log-${type}`;
        el.appendChild(div);
        el.scrollTop = el.scrollHeight;
    },
    setText: (id, text) => {
        document.getElementById(id).textContent = text;
    },
    setBtn: (id, enabled) => {
        document.getElementById(id).disabled = !enabled;
    }
};

window.addEventListener('securitypolicyviolation', event => {
    if (event.violatedDirective.startsWith('script-src'))
        state.lastCspViolation = event;
});

function base64UrlEncode(array) {
    return btoa(String.fromCharCode(...array))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
}

function sleep(ms) {
    return new Promise(resolve => window.setTimeout(resolve, ms));
}

function syncCspStatus() {
    const toggle = document.getElementById('cspToggle');
    const status = document.getElementById('cspStatus');

    toggle.checked = cspEnabled;
    status.textContent = cspEnabled
        ? '目前頁面已由伺服器送出真正的 CSP Header。關閉開關會重新載入成無 CSP 的示範頁面。'
        : '目前頁面未送出 CSP Header，僅供比較風險用。重新開啟會以新頁面重新載入。';

    toggle.addEventListener('change', () => {
        const url = new URL(window.location.href);
        if (toggle.checked)
            url.searchParams.delete('csp');
        else
            url.searchParams.set('csp', 'off');

        window.location.assign(url.toString());
    });
}

async function generatePkce() {
    const array = new Uint8Array(32);
    window.crypto.getRandomValues(array);
    state.verifier = base64UrlEncode(array);

    const hash = await window.crypto.subtle.digest('SHA-256', new TextEncoder().encode(state.verifier));
    state.challenge = base64UrlEncode(new Uint8Array(hash));
}

async function fetchAuthCode(username = '', password = '') {
    const hasSession = Boolean(state.loggedInUser);
    const res = await fetch(`${AUTH_SERVER}/authorize`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            username,
            password,
            clientId: DEMO_CLIENT_ID,
            redirectUri: REDIRECT_URI,
            codeChallenge: state.challenge,
            codeChallengeMethod: 'S256'
        })
    });

    if (!res.ok)
        throw new Error(await res.text());

    const data = await res.json();
    state.authCode = data.code;
    state.loggedInUser = data.username;
    UI.setText('valUser', state.loggedInUser);
    UI.log(hasSession ? 'Auth: Session 有效，免帳密核發 Code。' : 'Auth: 身分驗證通過。', 'warn');
    UI.log(`Auth -> SPA: 回傳 Authorization Code: ${state.authCode}`);
}

async function fetchToken() {
    const res = await fetch(`${AUTH_SERVER}/token`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            grantType: 'authorization_code',
            code: state.authCode,
            codeVerifier: state.verifier,
            clientId: DEMO_CLIENT_ID,
            redirectUri: REDIRECT_URI
        })
    });

    if (!res.ok)
        throw new Error(await res.text());

    const data = await res.json();
    state.token = data.access_token;
    UI.setText('valToken', '存於記憶體');
    UI.log('Auth: 驗證 SHA256(Verifier) == Challenge... 成功。', 'warn');
    UI.log(`Auth -> SPA: 核發 Access Token（${data.expires_in} 秒後到期）。`, 'sys');
}

// [Lab] 模擬限制：此函式本身在受信任的 emulator.js 內執行，邏輯上已有程式碼執行能力。
// 示範的是「動態建立 inline script 時 CSP script-src 'self' 的攔截行為」，
// 而非完整的 XSS 攻擊鏈（真實 XSS 是攻擊者注入的腳本在取得執行機會前就被阻擋）。
async function runInlineXssAttack() {
    UI.log('--- 遭受 XSS 攻擊 ---', 'error');
    UI.log('Hacker -> SPA: 注入 inline <script> 嘗試竊取記憶體中的 Token', 'error');

    delete window.__xssExecuted;
    delete window.__stolenToken;
    state.lastCspViolation = null;

    const script = document.createElement('script');
    script.text = "window.__xssExecuted = true; window.__stolenToken = window.__labState.token;";
    document.body.appendChild(script);
    await sleep(200);
    script.remove();

    if (window.__xssExecuted) {
        UI.log('SPA: 惡意腳本成功執行！', 'error');
        UI.log(`Hacker: 已竊取記憶體資料 Token: ${window.__stolenToken || '(空值)'}`, 'error');
        return;
    }

    if (state.lastCspViolation) {
        UI.log(`SPA (CSP): ⛔ 已阻擋 inline script。違規指令：${state.lastCspViolation.violatedDirective}`, 'warn');
        UI.log('防禦成功，記憶體中的 Token 安全無虞。', 'sys');
        return;
    }

    UI.log('SPA: 惡意腳本未執行，但沒有收到 CSP 違規事件。請檢查瀏覽器主控台。', 'warn');
}

function resetClientState() {
    state.verifier = '';
    state.challenge = '';
    state.authCode = '';
    state.token = '';
    state.loggedInUser = '';
    state.lastCspViolation = null;

    UI.setText('valUser', '-');
    UI.setText('valVerifier', '-');
    UI.setText('valToken', '-');

    document.getElementById('loginForm').classList.remove('visible');
    document.getElementById('inputUsername').value = '';
    document.getElementById('inputPassword').value = '';
    document.getElementById('consoleLog').innerHTML = '';

    UI.setBtn('btnPkce', true);
    UI.setBtn('btnAuth', false);
    UI.setBtn('btnToken', false);
    UI.setBtn('btnMe', false);
    UI.setBtn('btnXss', false);
    UI.setBtn('btnLogout', false);
}

document.getElementById('btnPkce').onclick = async () => {
    UI.log('SPA: 產生 PKCE Code Verifier 與 Challenge...', 'sys');
    await generatePkce();
    UI.setText('valVerifier', `${state.verifier.substring(0, 15)}...`);
    UI.log(`已備妥密語。Challenge: ${state.challenge.substring(0, 20)}...`, 'sys');
    UI.setBtn('btnPkce', false);
    UI.setBtn('btnAuth', true);

    if (!state.loggedInUser)
        document.getElementById('loginForm').classList.add('visible');
    else
        UI.log(`偵測到已登入 Session（${state.loggedInUser}），跳過帳密輸入。`, 'sys');
};

document.getElementById('btnAuth').onclick = async () => {
    const hasSession = Boolean(state.loggedInUser);
    const username = hasSession ? '' : document.getElementById('inputUsername').value.trim();
    const password = hasSession ? '' : document.getElementById('inputPassword').value;

    if (!hasSession && (!username || !password)) {
        UI.log('請輸入帳號與密碼', 'error');
        return;
    }

    UI.log(hasSession
        ? 'SPA -> Auth: 攜帶 Session Cookie、client_id 與 redirect_uri，請求 Authorization Code...'
        : `SPA -> Auth: 送出帳號「${username}」、client_id 與 redirect_uri，請求 Authorization Code...`);

    try {
        await fetchAuthCode(username, password);
        document.getElementById('loginForm').classList.remove('visible');
        UI.setBtn('btnAuth', false);
        UI.setBtn('btnToken', true);
        UI.setBtn('btnLogout', true);
    } catch (err) {
        UI.log(`錯誤：${err.message}`, 'error');
    }
};

document.getElementById('btnToken').onclick = async () => {
    UI.log('SPA -> Auth: 發送 Code、Verifier、client_id 與 redirect_uri 交換 Token...');
    try {
        await fetchToken();
        UI.setBtn('btnToken', false);
        UI.setBtn('btnMe', true);
        UI.setBtn('btnXss', true);
    } catch (err) {
        UI.log(`錯誤：${err.message}`, 'error');
    }
};

document.getElementById('btnMe').onclick = async () => {
    UI.log('SPA -> API: 攜帶 Bearer Token 呼叫 GET /api/me...');
    try {
        const res = await fetch(`${AUTH_SERVER}/api/me`, {
            headers: { Authorization: `Bearer ${state.token}` }
        });

        if (!res.ok)
            throw new Error(await res.text());

        const data = await res.json();
        UI.log(`API -> SPA: ${data.message}`, 'warn');
        UI.log('受保護資源存取成功。', 'sys');
    } catch (err) {
        UI.log(`錯誤：${err.message}`, 'error');
    }
};

document.getElementById('btnXss').onclick = async () => {
    await runInlineXssAttack();
};

document.getElementById('btnLogout').onclick = async () => {
    try {
        await fetch(`${AUTH_SERVER}/logout`, { method: 'POST', credentials: 'include' });
        resetClientState();
        UI.log('Auth: Session 已移除，Cookie 已清除。', 'sys');
        UI.log('請點擊「1. 產生 PKCE 密語」重新登入。', 'sys');
    } catch (err) {
        UI.log(`登出失敗：${err.message}`, 'error');
    }
};

syncCspStatus();

(async () => {
    try {
        const res = await fetch(`${AUTH_SERVER}/authorize/session`, { credentials: 'include' });
        if (!res.ok) {
            UI.log(`系統就緒。AuthServer: ${AUTH_SERVER}`, 'sys');
            UI.log('請點擊「1. 產生 PKCE 密語」開始。', 'sys');
            return;
        }

        const { username } = await res.json();
        state.loggedInUser = username;
        UI.setText('valUser', username);
        UI.log(`偵測到已登入 Session（${username}），靜默恢復 Token 中...`, 'warn');

        await generatePkce();
        UI.log('SPA: 自動產生新的 PKCE 密語...', 'sys');

        await fetchAuthCode();
        UI.log('SPA -> Auth: 以 Session Cookie 取得 Authorization Code...', 'sys');

        await fetchToken();
        UI.setBtn('btnPkce', false);
        UI.setBtn('btnAuth', false);
        UI.setBtn('btnToken', false);
        UI.setBtn('btnLogout', true);
        UI.setBtn('btnMe', true);
        UI.setBtn('btnXss', true);
        UI.log('Token 已恢復，可直接呼叫受保護 API。', 'sys');
    } catch (err) {
        UI.log(`系統就緒（Session 恢復失敗：${err.message}）`, 'sys');
        UI.log('請點擊「1. 產生 PKCE 密語」開始。', 'sys');
    }
})();
