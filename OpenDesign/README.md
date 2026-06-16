# Open Design 開發環境（WSL2）

這個資料夾是 Open Design 在 WSL2 環境下的開發/執行設定，**不是** Open Design 本身的原始碼（原始碼另外
clone 在 `open-design/` 子資料夾，見 `INSTALL.md`）。提供兩種執行模式：

| 模式 | 適合情境 | 進入點 |
|---|---|---|
| **Dev 模式** | 直接跑原始碼，能偵測本機 `claude` CLI，免費用 Claude Code 訂閱額度 | `./od-cli.sh start` |
| **Docker 模式** | 一次性容器內建 Claude / Copilot / Codex / Antigravity 四個 AI CLI | `./od-cli.sh build`／`shell`／`run` |

兩種模式統一用根目錄的 `od-cli.sh` 管理。詳細安裝步驟、除錯紀錄、已知限制都在 `INSTALL.md`，這裡只列快速指令。

## Dev 模式

```bash
./od-cli.sh start
```

啟動後開 `http://localhost:3000`。`start` 會自動 `git pull`、在 lockfile 有變動時自動 `pnpm install`，
daemon 跟 web 都背景啟動，執行完直接返回終端機。用完執行 `./od-cli.sh stop` 關閉 daemon + web。

## Docker 模式

每次 `shell`/`run` 都是一次性容器（`docker run --rm`），跑完自動清除，不需要額外 `start`/`stop`：

```bash
./od-cli.sh build                          # 建置 image（第一次或 Dockerfile 改動後）
./od-cli.sh run <claude|copilot|codex|agy>  # 一次性容器，執行指定 CLI
./od-cli.sh shell                            # 或一次性容器，進去裸 bash
```

**必須在真正的終端機視窗執行**（不能透過 Claude Code 對話框的 `!` 前綴，沒有真 TTY 會報錯）。

細節（CLI 清單、憑證位置、登入步驟、WSL2 已知限制）見 `.devcontainer/README.md`。

## 同步到 sample.dotblog repo

這個資料夾的內容需要手動同步一份到 `/mnt/d/lab/sample.dotblog/OpenDesign`：

```bash
./sync-file.sh
```

單向同步（這裡 → 那裡），不會刪除對方多出來的檔案。

## 資料夾結構

完整結構見 `tree.md`（隨檔案異動同步維護）。

## 問題紀錄與計畫存檔

- `.issues/` — 過程中遇到的問題、根因、解法，避免重複踩坑
- `.archive/` — 已完成的實作計畫
