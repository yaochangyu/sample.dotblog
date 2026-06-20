---
title: '[Open Design] 在 WSL2 / Linux 本地環境啟動 Open Design'
abstract: <p><a target="_blank" rel="noopener noreferrer" href="https://github.com/nexu-io/open-design">Open Design</a> 是開源的 Claude Design 替代方案，支援 100+ 技能、150 個設計系統、261 個 Plugin，可以串接 Claude Code、Cursor、Copilot 等 21 種 Coding Agent，可直接使用本機環境的授權配置，也支援自備 API Key（BYOK）。這篇記錄在 WSL2 上從 clone 到跑起來的完整過程，以及踩到的坑。</p><figure class="image"><img style="aspect-ratio:1376/768;" src="https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781965423.jpg.jpg" width="1376" height="768"></figure>
keywords: Open Design
categories: Open Design
weblogName: 余小章 @ 大內殿堂
postId: 0b6cbf33-82ec-4db1-849f-f61e26c58a25
postDate: 2026-06-20T06:00:53.0000000
postStatus: 
dontInferFeaturedImage: false
stripH1Header: true
---
# [Open Design] 在 WSL2 / Linux 本地環境啟動 Open Design

## 開發環境

- OS：Windows 11 + WSL2 Ubuntu 24.04
- Node.js：24（透過 nvm）
- pnpm：10.33.2（透過 corepack 自動選用）
- open-design：https://github.com/nexu-io/open-design

---

## Open Design 架構

Open Design 跑起來有兩個 process：

| Process | Port | 說明 |
| --- | --- | --- |
| **daemon** | 7456 | 後端核心，處理 AI 呼叫、Plugin、CLI 整合 |
| **web** | 3000 | Next.js 前端，使用者操作介面 |

兩個都要同時跑，daemon 先起來，web 才能正常運作。

Daemon 是整個系統的核心，Web 前端只是 UI，所有實際工作都在 daemon：

- **AI 呼叫**：把 prompt 轉發給 Claude Code / OpenAI / Gemini 等，回傳結果給前端
- **CLI 整合**：偵測 PATH 上的 `claude`、`cursor` 等工具，讓 Open Design 能驅動它們
- **Plugin 執行**：管理 261 個 Plugin 的安裝與呼叫
- **檔案系統操作**：選擇工作目錄、讀寫檔案
- **Settings 管理**：API Key、執行模式等設定

前端沒有 daemon 就是個空殼。

---

## 安裝前置需求

```
# 安裝 nvm
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.7/install.sh | bash

# 安裝 Node 24
nvm install 24
nvm use 24

# 啟用 corepack（pnpm 透過它自動選版本）
corepack enable

# 安裝 zenity（WSL2 限定，選擇工作目錄功能需要）
sudo apt install -y zenity
```

---

## 安裝 Open Design

### 1. Clone 原始碼

```
git clone https://github.com/nexu-io/open-design open-design
cd open-design
```

### 2. 安裝依賴

```
pnpm install
```

首次約需 **8~10 分鐘**，下載約 952 個套件，完成後會自動 build 所有內部套件（contracts → daemon → web 等 16 個）。

預期最後幾行：

```
. postinstall: Done
╭ Warning ────────────────────────────────╮
│  Ignored build scripts: node-pty@1.1.0  │
╰─────────────────────────────────────────╯
Done in 8m 32.7s using pnpm v10.33.2
```

> `node-pty` 警告可忽略，不影響主要功能。

---

### 為什麼不直接用 `pnpm tools-dev run web`？

官方開發模式是透過 `tools-dev` 同時啟動 daemon + web，兩者用 POSIX Unix Domain Socket（IPC）通訊。WSL2 環境下這個 socket 建立常常超時：

```
daemon did not expose status in time
```

解法是分開啟動，繞過 IPC 等待。

---

## 手動啟動

先確認可以手動跑起來：

```
# 終端機 1：啟動 daemon
cd open-design
OD_WEB_PORT=3000 node apps/daemon/dist/cli.js --port 7456 --no-open

# 終端機 2：啟動 web
cd open-design
PORT=3000 OD_PORT=7456 pnpm --filter @open-design/web dev
```

`OD_WEB_PORT=3000` 必填，告訴 daemon 信任來自 Next.js（port 3000）的請求，少了它 Settings 頁面測試 CLI 會出現 **「Daemon responded with 403」**。

開啟 **http://localhost:3000** 看到介面就成功了。

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781944343.png.png)

---

## 用 Python 腳本統一管理

每次要開兩個終端機、記環境變數、等待順序，太麻煩了。寫了一支 `od-cli.py` 統一管理，用 `uv run` 執行，自動處理依賴（`psutil`），支援 Windows / WSL2 / Linux。

### 前置需求

```
# Python 3.11+（系統通常已內建，確認一下）
python3 --version

# 安裝 uv
curl -LsSf https://astral.sh/uv/install.sh | sh   # Linux/WSL
# winget install astral-sh.uv                      # Windows
```

### 安裝

把 `od-cli.py` 放在 `open-design/` 的上一層目錄（或任意位置，腳本會在自己所在目錄找 `open-design/` 子資料夾）。

### 日常操作

啟動 daemon + web（背景執行，等待 health check 通過後返回）：

```
uv run od-cli.py start
```

```
Starting daemon on port 7456...
Daemon PID: 32619
Daemon ready (3s).

Starting web on port 3000...
Web PID: 33032
Web ready (4s).

Open: http://localhost:3000
```

查看服務狀態（PID、uptime、記憶體、health check）：

```
uv run od-cli.py status
```

```
daemon   [running]  port=7456  PID=32619  uptime=01:23:45  started=2026-06-20 13:43:33
                     mem=177.4MB  cpu=0.0%
web      [running]  port=3000  PID=33032  uptime=01:23:40  started=2026-06-20 13:43:38
                     mem=590.2MB  cpu=0.0%

daemon health: {"ok":true,"version":"0.11.0"}
```

停止：

```
uv run od-cli.py stop
```

取得最新版（git pull + 視情況重裝依賴 + rebuild daemon）：

```
uv run od-cli.py update
```

---

## 選擇模型

服務啟動後，執行 http://localhost:3000

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781964864.png.png)

選擇本機 CLI，選擇重新掃描，他就會讀取本機的 cli 設定

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781964896.png.png)

選擇好模型，就可以開始產生 UI

```
我：
我要設計一個 event-bus 的非同步平台介面
```

NOTE：這是很粗糙的需求描述

```
AI：
詢問我更多的問題、方向
```

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781967298.png.png)

完成之後，出現一個 html 的頁面，這時候就可以點選右邊的工具

- 編輯 html

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781967325.png.png)

- 窗選某一個區塊跟 AI 互動

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781967342.png.png)

- 選擇某一個區塊跟 AI 互動

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781967358.png.png)

---

## 射茶包：選擇工作目錄點了沒反應

Open Design 介面左側有個「選擇工作目錄」，可以讓 daemon 知道 Claude Code 要操作哪個資料夾。在 WSL2 上點下去完全沒反應，不跳視窗、不報錯，就這樣。

第一步先直接打 daemon API 確認：

```
curl -X POST http://localhost:7456/api/dialog/open-folder
# {"path":null}
```

立刻回傳 `null`，根本沒等使用者操作。代表 daemon 呼叫某個程式失敗，直接 resolve null 回去了。

翻 daemon 原始碼，找到 `server.ts` 裡的 `openNativeFolderDialog`：

```
platform === 'linux' → execFile('zenity', ['--file-selection', '--directory', ...])
```

WSL2 的 `process.platform` 是 `linux`，所以走 zenity 分支。確認一下：

```
zenity --version
# zenity: command not found
```

裝起來就解了：

```
sudo apt install -y zenity
```

裝完回 Open Design 點選擇工作目錄，資料夾選擇視窗正常彈出。

---

## 已知問題

| 問題 | 原因 | 解法 |
| --- | --- | --- |
| 選擇工作目錄點了沒反應（WSL2） | daemon 呼叫 `zenity` 開資料夾選擇器，WSL2 預設未安裝 | `sudo apt install -y zenity` |
| Settings 測試 CLI 出現 Daemon responded with 403 | daemon 啟動未設定 `OD_WEB_PORT` | 用 `od-cli.py start`（已自動帶入） |
| `pnpm tools-dev run web` 超時 | WSL2 IPC socket 不穩定 | 改用 `od-cli.py start` |

---

## Open Design UI 實作範例

這裡我用 event-bus 的案例來示範。

- Model：選 Claude，執行的效果還不錯；相同的提示詞，antigravify 就看看就好。

我：

```
建立 MQ Platform/Event-Bus 集中管理平台，需要用以下功能
- 用 rabbit 實現
- 用 pub/sub api 建立 task
- 可管理 task 配置
    - 管理 queue
    - 配置 callback，callback 使用 API
    - 配置 SLO，callback 花費時間，多久時間沒有完成，狀態設定 timeout
    - 支援三種類型，event、task、scheduler，每一個類型都可以是
        - event：通知訂閱者
        - task：回呼 api
        - scheduler(延遲執行 task)：指定時間，回呼 api
- pub api，
    - 建立 task，task 有區分立即執行和延遲執行。
    - 建立 event，event 再根據有哪些訂閱者，執行 task。
- register api，
    - 訂閱事件
- callback api 必須要能回報，啟動時間、結束時間，以及任何錯誤訊息

你會怎麼設計
```

AI 問我幾個問題，我回答後

```
[form answers — discovery] - 目標平台: 響應式網頁（支援平板與手機） 
[value: responsive-web] - 視覺風格: Modern Minimal（Linear / Vercel 風格，乾淨精緻） 
[value: modern-minimal] - 要包含哪些畫面？: Dashboard 總覽（Queue 狀態、訊息量、SLO 健康度）
[value: dashboard], Task 管理（建立 / 編輯 Task，callback、SLO 設定）
[value: task-management], Event 管理（訂閱者列表、pub 紀錄） 
[value: event-management], Scheduler 管理（排程任務、延遲執行紀錄）
[value: scheduler], Queue 管理（Queue 列表、死信佇列） 
[value: queue-management], Callback 執行紀錄（啟動時間、結束時間、錯誤訊息） 
[value: callback-logs] - 品牌設定: (skipped) - 其他限制或補充說明: (skipped)
```

過了將近 20min 就產出了基本的頁面，稍微執行了一下，感覺還可以

![](https://dotblogsfile.blob.core.windows.net/user/余小章/0b6cbf33-82ec-4db1-849f-f61e26c58a25/1781966425.png.png)

---

## 完整代碼位置

https://github.com/yaochangyu/sample.dotblog/tree/master/OpenDesign
