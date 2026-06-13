# Claude 串接本地 Gemma 4 原生環境實驗計畫

本計畫旨在 Linux 原生檔案系統（`/home/yao/projects/claude-gemma4-experiment`）下，使用優化後的地端模型 `gemma4-opt` 與 Claude Code 進行高速實驗與 Tool Calling 功能驗證。

- [x] **步驟 1：在原生 Linux 環境下啟動優化後的地端 Claude Code 服務** <!-- id: 0 -->
  - **原因**：驗證在 Linux 原生 I/O 下，指向本地 LiteLLM 代理的 `gemma4-opt` 連線是否順暢無阻。
- [x] **步驟 2：執行 /init 專案初始化任務** <!-- id: 1 -->
  - **原因**：測試模型在高速 I/O 環境下，針對多檔案的讀取與 CLAUDE.md 生成的 Tool use 效率與成功率。
- [x] **步驟 3：進行代碼生成與 Bug 修正的實際開發測試** <!-- id: 2 -->
  - **原因**：在本地專案目錄內修改一個簡單的程式，驗證模型是否能穩定遵循繁體中文，且避免暴力覆寫 settings.json 或其它檔案。
- [x] **步驟 4：彙整實驗效能與操作指南，並將本計畫移至 `.archive` 歸檔** <!-- id: 3 -->
  - **原因**：將實驗成果回寫至主要工作區，以維護文件完整性，並將計畫歸檔。
