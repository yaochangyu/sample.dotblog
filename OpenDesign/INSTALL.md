# Open Design 安裝手冊（WSL2 Dev 模式）

> 適用環境：Windows WSL2 / Linux  
> Node 24 + pnpm 10.33.2 + Claude Code CLI

---

## 目錄結構

```
OpenDesign/            ← 這個資料夾（od-cli.sh 所在位置）
└── open-design/       ← open-design 原始碼（需另外 clone）
```

`od-cli.sh` 會在自己所在目錄底下找 `open-design/` 子資料夾。

---

## 前置需求

| 工具 | 版本 | 安裝指令 |
|------|------|----------|
| Node.js | ~24 | `nvm install 24` |
| pnpm | 10.33.2（透過 corepack 自動選用） | `corepack enable` |
| Claude Code | 任意版本 | `curl -fsSL https://claude.ai/install.sh \| bash` |
| git | 任意 | 系統內建 |

---

## 一次性安裝（只需執行一次）

### Step 1 — Clone repo

在 `od-cli.sh` 所在目錄執行：

```bash
git clone https://github.com/nexu-io/open-design open-design
```

### Step 2 — 切換 Node 版本並啟用 corepack

```bash
cd open-design
source ~/.nvm/nvm.sh
nvm use 24
corepack enable
corepack pnpm --version   # 應印出 10.33.2
```

### Step 3 — 安裝依賴

```bash
pnpm install
```

> 首次約需 **8~10 分鐘**，下載 952 個套件。  
> 完成後會自動 build 所有內部套件（contracts、daemon、tools-dev 等）。

**安裝摘要（參考值）：**

| 項目 | 數量 |
|------|------|
| Workspace 專案數 | 24 |
| 解析套件數 | 952 |
| 下載 | 775 |
| 耗時 | 約 8~10 分鐘 |

**postinstall 自動 build 的內部套件：**  
`contracts` → `components` → `platform` → `download` → `host` → `registry-protocol` → `agui-adapter` → `plugin-runtime` → `sidecar-proto` → `launcher-proto` → `sidecar` → `diagnostics` → `daemon` → `tools-dev` → `tools-pack` → `tools-serve`

**預期最後幾行：**
```
. postinstall: Done
╭ Warning ────────────────────────────────╮
│  Ignored build scripts: node-pty@1.1.0  │
╰─────────────────────────────────────────╯
Done in 8m 32.7s using pnpm v10.33.2
```

> `node-pty` 警告可忽略，不影響主要功能（node-pty 用於終端機模擬）。

---

## 日常操作

### 啟動

```bash
./od-cli.sh start
```

啟動後開啟瀏覽器：**http://localhost:3000**。daemon 跟 web 都背景啟動，指令執行完會直接返回終端機。

### 停止

```bash
./od-cli.sh stop
```

用 port 反查 PID，關閉 daemon（7456）+ web（3000）。

### 取得最新版本

```bash
./od-cli.sh update
```

執行 `git pull` 取最新版，若 `pnpm-lock.yaml` 有異動則自動重裝依賴（postinstall 會一併 rebuild 所有內部套件）；若 lockfile 沒變則只 rebuild daemon。完成後執行 `start` 啟動。

---

## 手動啟動步驟

若 `od-cli.sh start` 失敗，依序執行以下指令：

### 1. 切換 Node 版本

```bash
source ~/.nvm/nvm.sh && nvm use 24
```

### 2. 啟動 Daemon（背景執行）

```bash
cd open-design
OD_WEB_PORT=3000 node apps/daemon/dist/cli.js --port 7456 --no-open > /tmp/od-daemon.log 2>&1 &
echo "Daemon PID: $!"
```

> `OD_WEB_PORT=3000` 必填：告訴 daemon 信任來自 Next.js dev server（port 3000）的請求。  
> 若未設定，Settings 頁面測試 CLI 會出現 **「Daemon responded with 403」**。

確認 daemon 正常：

```bash
curl -s http://localhost:7456/api/health
# 預期輸出: {"ok":true,"version":"0.10.0"}
```

### 3. 啟動 Web 前端

```bash
PORT=3000 OD_PORT=7456 pnpm --filter @open-design/web dev
```

開啟 **http://localhost:3000**

---

## 為什麼不用 `pnpm tools-dev run web`？

`tools-dev` 透過 POSIX Unix Domain Socket（IPC）與 daemon 通訊，  
在 WSL2 環境下 IPC socket 建立有時會逾時，造成：

```
daemon did not expose status in time
```

**解決方式**：直接啟動 daemon 與 web，繞過 `tools-dev` 的 IPC 等待。

---

## 執行模式

| 模式 | 設定位置 | 說明 |
|------|----------|------|
| **Local CLI**（預設） | Settings → Execution mode | Daemon 偵測 PATH 上的 `claude`，免費使用 Claude Code 訂閱額度 |
| **API mode（BYOK）** | Settings → Execution mode | 輸入 Anthropic / OpenAI / Gemini API Key，按 Token 計費 |

---

## 除錯紀錄

| 問題 | 原因 | 解法 |
|------|------|------|
| Settings 測試 CLI 出現 **Daemon responded with 403** | 啟動 daemon 未設定 `OD_WEB_PORT`，daemon 拒絕來自 Next.js（port 3000）的請求 | 啟動 daemon 時加上 `OD_WEB_PORT=3000` |
| `pnpm tools-dev run web` 失敗 | WSL2 IPC socket 逾時 | 改用手動分別啟動 daemon + web |
| `node-pty` build 警告 | pnpm 安全機制攔截，非核心功能 | 忽略 |

---

## 停止服務

```bash
./od-cli.sh stop
```

若 `od-cli.sh stop` 失敗，可手動反查並關閉：

```bash
ss -tlnp | grep -E ':(7456|3000)\s'   # 找出佔用該 port 的 PID
kill <PID>
```
