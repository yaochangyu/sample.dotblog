# Open Design 安裝手冊（WSL2 Dev 模式）

> 適用環境：Windows WSL2 / Linux  
> Node 24 + pnpm 10.33.2 + Claude Code CLI

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

```bash
git clone https://github.com/nexu-io/open-design /mnt/d/lab/sample.dotblog/OpenDesign/open-design
cd /mnt/d/lab/sample.dotblog/OpenDesign/open-design
```

### Step 2 — 切換 Node 版本並啟用 corepack

```bash
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

## 每次啟動（使用 od-cli.sh）

```bash
/mnt/d/lab/sample.dotblog/OpenDesign/od-cli.sh start
```

啟動後開啟瀏覽器：**http://localhost:3000**。daemon 跟 web 都背景啟動，指令執行完會直接返回終端機。

> **為什麼每次啟動都會 `git pull`？**  
> 目前使用的是 **Dev 模式**（直接跑原始碼），不是 Docker。  
> Dev 模式的優點是能偵測本機的 `claude` CLI；代價是需要自己維護原始碼更新。  
> `od-cli.sh start` 在啟動前自動 `git pull`，確保每次都是最新版本。  
> 若 `pnpm-lock.yaml` 有異動，也會自動執行 `pnpm install` 同步依賴。

用完執行 `od-cli.sh stop` 關閉 daemon + web（見「停止服務」章節）。

---

## 手動啟動步驟

若 `od-cli.sh start` 失敗，依序執行以下指令：

### 1. 切換 Node 版本

```bash
source ~/.nvm/nvm.sh && nvm use 24
```

### 2. 啟動 Daemon（背景執行）

```bash
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

## 除錯紀錄

| 問題 | 原因 | 解法 |
|------|------|------|
| Settings 測試 CLI 出現 **Daemon responded with 403** | 啟動 daemon 未設定 `OD_WEB_PORT`，daemon 拒絕來自 Next.js（port 3000）的請求 | 啟動 daemon 時加上 `OD_WEB_PORT=3000` |
| `pnpm tools-dev run web` 失敗 | WSL2 IPC socket 逾時 | 改用手動分別啟動 daemon + web |
| `npm install nexu-io/open-design` 失敗 | 套件是 `private: true` monorepo，含 `workspace:*` 依賴 | 需 clone 整個 repo |
| Docker 模式無法用 Local CLI（**已解決，見下方「Docker 模式」**） | 容器內看不到 host 的 `claude` binary | 改在 `.devcontainer/Dockerfile` 內直接安裝 CLI |
| `node-pty` build 警告 | pnpm 安全機制攔截，非核心功能 | 忽略 |

---

## 兩種執行模式說明

| 模式 | 設定位置 | 說明 |
|------|----------|------|
| **Local CLI**（預設） | Settings → Execution mode | Daemon 偵測 PATH 上的 `claude`，免費使用 Claude Code 訂閱額度 |
| **API mode（BYOK）** | Settings → Execution mode | 輸入 Anthropic / OpenAI / Gemini API Key，按 Token 計費 |

---

## Docker 模式（內建四個 AI CLI）

> 設定位置：`.devcontainer/Dockerfile`、`.devcontainer/devcontainer.json`；管理入口是根目錄的 `od-cli.sh`
> 詳細實作計畫與步驟見 `.archive/docker-ai-cli.plan.md`（已完成並封存）

容器內已安裝並**驗證登入成功**四個 AI CLI：

| CLI | 安裝方式 | binary 名稱 | 憑證實際存放位置（volume 內） |
|---|---|---|---|
| Claude Code | `curl install.sh \| bash` | `claude` | `~/.claude/.credentials.json` |
| GitHub Copilot CLI | `npm install -g @github/copilot` | `copilot` | `~/.copilot/config.json` |
| OpenAI Codex CLI | `npm install -g @openai/codex` | `codex` | `~/.codex/auth.json` |
| Google Antigravity CLI | 官方安裝腳本 | `agy`（**不是** `antigravity`） | `~/.gemini/antigravity-cli/antigravity-oauth-token`（**不是**走 keyring，見下方說明） |

### 建置與啟動

統一用根目錄的 `od-cli.sh` 管理（需在**真正的終端機視窗**執行，見下方「已知限制」）：

每次 `shell`/`run` 都是一次性容器（`docker run --rm`），跑完自動清除，不需要額外 `start`/`stop`：

```bash
./od-cli.sh build                          # 建置 image
./od-cli.sh shell                            # 一次性容器，互動式 bash（離開即自動清除）
./od-cli.sh run <claude|copilot|codex|agy>   # 一次性容器，執行指定 CLI（必填，不給會報錯）
```

也可透過 VS Code「Dev Containers: Reopen in Container」開啟，會自動套用 `devcontainer.json` 的 volume mount 設定。容器用 `--network host`（見下方「已知限制」）。

### 登入

四個 CLI 的登入方式：

```bash
./od-cli.sh run claude    # 貼代碼模式：依畫面指示開啟瀏覽器或按 c 複製網址，完成後貼代碼回終端
./od-cli.sh run copilot   # 瀏覽器 OAuth callback，正常會自動完成
./od-cli.sh run codex     # 瀏覽器 OAuth callback，WSL2 環境下會卡住，見下方「已知限制」的處理方式
./od-cli.sh run agy       # 貼代碼模式：Antigravity CLI，依畫面指示完成 Google 帳號登入
```

登入後憑證會落在上表對應的 volume 路徑，容器重建（rebuild）不需要重新登入（已實測驗證）。

### 已知限制

- **`od-cli.sh` 的 `shell`/`run` 必須在真正的終端機視窗執行**：Claude Code 對話框內無論用 `!` 前綴或工具呼叫，都沒有提供真正的 TTY，`docker run -it` 會出現 `the input device is not a TTY`。
- **`codex` 在 WSL2 環境下，瀏覽器登入到最後一步會顯示「localhost 拒絕連線」**：`codex` 的 OAuth callback server 綁定容器內 `127.0.0.1`，Windows 瀏覽器透過 WSL2 的 localhost 轉送是從 WSL2 對外網路介面進來，連不到只聽 loopback 的服務，跟 Docker 的網路設定（`-p`／`--network host`）無關，調 Docker 設定解不掉。
  **處理方式**：瀏覽器卡住「拒絕連線」時，把網址列那串完整網址（含 `code=`、`scope=`、`state=`）複製下來，在 **WSL2 終端機**（不是容器內）執行：
  ```bash
  curl "<剛剛複製的完整網址>"
  ```
  收到 `302` 即代表登入完成。
- **Antigravity 的 keyring 機制（`gnome-keyring` + `dbus`）目前驗證下來是多做的**：實測登入後，token 是直接寫入 `~/.gemini/antigravity-cli/antigravity-oauth-token` 一般檔案，`~/.local/share/keyrings` volume 完全是空的。Antigravity 在偵測不到/不需要真正 keyring 時會優雅退回檔案儲存。保留 `start-keyring.sh` 不會造成壞處，但不是登入成功的必要條件。

### 除錯紀錄（Docker 模式）

| 問題 | 原因 | 解法 |
|---|---|---|
| `npm install -g @github/copilot` 出現 `EACCES` | nvm/Node 安裝階段是用 `root` 執行，導致 npm 全域目錄屬主是 root，後續切換 `vscode` 使用者沒有寫入權限 | Dockerfile 在切換 `USER vscode` 前加 `chown -R vscode:vscode "$NVM_DIR"` |
| `docker run -it` 出現 `the input device is not a TTY` | Claude Code 對話框的執行管道沒有真正的 TTY | 改在真正的終端機視窗執行 `od-cli.sh` |
| `agy` 啟動時 `permission denied`（log/installation_id） | `~/.gemini/antigravity-cli`、`~/.local/share/keyrings` 在 image build 時不存在，named volume 第一次掛載被建成 root 屬主 | Dockerfile 預先 `mkdir -p` 這些目錄（vscode 身分）；已存在的舊 volume 用一次性 `busybox chown -R 1000:1000` 修正 |
| `codex` 啟動時 sqlite `unable to open database file` | 同上，`~/.codex`、`~/.copilot` 也是 image build 時不存在的目錄 | 同上解法，補進同一行 `mkdir -p` |
| `copilot` 登入失敗，瀏覽器卡在 `localhost:1455/auth/callback` | Copilot 的瀏覽器 OAuth 在容器內監聽 1455 接收 callback，未發布到 host | 改用 `--network host` |
| `codex` 加了 `--network host` 仍是「拒絕連線」 | 真正根因是 WSL2 ↔ Windows 的 localhost 轉送機制連不到綁定 `127.0.0.1` 的服務，跟 Docker 網路設定無關 | 複製失敗網址，在 WSL2 終端機直接 `curl` 該網址完成登入 |

> 完整失敗紀錄與排查過程見 `.issues/docker-ai-cli.issues.md`；實作計畫見 `.archive/docker-ai-cli.plan.md`（已全部完成並封存）。

---

## 停止服務

```bash
./od-cli.sh stop   # 用 port 反查 PID，關閉 daemon（7456）+ web（3000）
```

若 `od-cli.sh stop` 失敗，可手動反查並關閉：

```bash
ss -tlnp | grep -E ':(7456|3000)\s'   # 找出佔用該 port 的 PID
kill <PID>
```
