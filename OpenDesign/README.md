# Open Design 開發環境

這個資料夾是 Open Design 的設定與管理腳本，**不是** Open Design 本身的原始碼。  
原始碼需另外 clone 成 `open-design/` 子資料夾（見 `INSTALL.md`）。

> 支援 Windows / WSL2 / Linux，透過 `uv run` 跨平台執行。

## 快速指令

```bash
uv run od-cli.py start    # 背景啟動 daemon + web，開啟 http://localhost:3000
uv run od-cli.py stop     # 關閉 daemon + web
uv run od-cli.py update   # git pull 取最新版 + 視情況重裝依賴 + rebuild daemon
uv run od-cli.py status   # 顯示 daemon + web 執行狀態
```

詳細安裝步驟、除錯紀錄見 `INSTALL.md`。

## 資料夾結構

完整結構見 `tree.md`（隨檔案異動同步維護）。

## 問題紀錄

- `.issues/` — 過程中遇到的問題、根因、解法，避免重複踩坑
