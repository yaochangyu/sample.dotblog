# Docker 模式：內建四個 AI CLI

容器內已安裝並**驗證登入成功**四個 AI CLI，供 Open Design 在 Docker 環境下使用。

> 完整安裝計畫見 `../.archive/docker-ai-cli.plan.md`（已完成並封存）
> 完整除錯紀錄見 `../.issues/docker-ai-cli.issues.md`（6 個 issue，含根因與解法）

## CLI 清單

| CLI | binary 名稱 | 憑證實際存放位置（volume 內） |
|---|---|---|
| Claude Code | `claude` | `~/.claude/.credentials.json` |
| GitHub Copilot CLI | `copilot` | `~/.copilot/config.json` |
| OpenAI Codex CLI | `codex` | `~/.codex/auth.json` |
| Google Antigravity CLI | `agy`（**不是** `antigravity`） | `~/.gemini/antigravity-cli/antigravity-oauth-token`（不是走 keyring） |

## 指令（根目錄的 `od-cli.sh`）

每次 `shell`/`run` 都是一次性容器（`docker run --rm`），跑完自動清除，不需要額外 `start`/`stop`：

```bash
../od-cli.sh build                          # 建置 image
../od-cli.sh shell                            # 一次性容器，互動式 bash（離開即自動清除）
../od-cli.sh run <claude|copilot|codex|agy>   # 一次性容器，執行指定 CLI（必填，不給就報錯）
```

容器設定：`--network host`（見「已知限制」）。`od-cli.sh` 同時也管 Dev 模式的 `start`/`stop`，
跟這支容器無關，見專案根目錄 `README.md`。

## ⚠️ 必須在真正的終端機視窗執行

Claude Code 對話框內（不管是 `!` 前綴還是工具呼叫）都沒有真正的 TTY，`docker run -it` 會出現
`the input device is not a TTY`。四個 CLI 的互動式登入畫面都需要真 TTY，**必須**在 WSL2 終端機、
Windows Terminal 等真正的終端機視窗執行。

## 登入

```bash
../od-cli.sh run claude    # 貼代碼模式：依畫面指示開啟瀏覽器或按 c 複製網址，完成後貼代碼回終端
../od-cli.sh run copilot   # 瀏覽器 OAuth callback，正常會自動完成
../od-cli.sh run codex     # 瀏覽器 OAuth callback，WSL2 環境下會卡住，見下方處理方式
../od-cli.sh run agy       # 貼代碼模式：依畫面指示完成 Google 帳號登入
```

登入後憑證會落在上表對應的 volume 路徑，容器重建（rebuild）不需要重新登入（已實測驗證）。

### codex 在 WSL2 環境卡在「localhost 拒絕連線」怎麼辦

`codex` 的 OAuth callback server 綁定容器內 `127.0.0.1`，Windows 瀏覽器透過 WSL2 的 localhost
轉送是從 WSL2 對外網路介面進來，連不到只聽 loopback 的服務，跟 Docker 的網路設定無關，調
Docker 設定解不掉。

處理方式：瀏覽器卡住「拒絕連線」時，把網址列那串完整網址（含 `code=`、`scope=`、`state=`）複製下來，
在 **WSL2 終端機**（不是容器內）執行：

```bash
curl "<剛剛複製的完整網址>"
```

收到 `302` 即代表登入完成。

## 檔案說明

| 檔案 | 用途 |
|---|---|
| `Dockerfile` | Ubuntu base + Node 24 + 四個 CLI 安裝 |
| `devcontainer.json` | VS Code Dev Containers 設定，四組憑證 named volume |
| `start-keyring.sh` | 容器內模擬桌面 keyring（dbus + gnome-keyring）；**實測後確認非必要**，Antigravity 會優雅退回檔案儲存，保留無壞處 |

> 容器管理入口 `od-cli.sh` 在專案根目錄（不在這裡），同時管 Dev 模式啟停跟這裡的 AI CLI 容器。

## 已知限制

- `od-cli.sh` 的 `shell`/`run` 必須在真正的終端機視窗執行（見上方說明）。
- `codex` 在 WSL2 環境下的瀏覽器登入需要額外的 `curl` 步驟（見上方說明）。
- `--network host` 僅支援 Linux（WSL2 沒問題），macOS/Windows 原生 Docker Desktop 不支援，需另外處理。
