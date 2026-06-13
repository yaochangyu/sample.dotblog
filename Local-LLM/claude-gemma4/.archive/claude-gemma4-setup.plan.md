# Claude 串接本地 Gemma 4 實作與驗證計畫

本計畫旨在按照最佳實踐，在本地環境完成 Claude Code 串接地端 Gemma 4 模型的實作、驗證，並撰寫操作指南。

- [x] **步驟 1：建立擴展上下文的自訂 Ollama Gemma 4 模型** <!-- id: 0 -->
  - **原因**：解決 Ollama 預設 4k 上下文不足的問題。需撰寫 Modelfile 將 `num_ctx` 設定為 `32768` 並利用 `ollama create` 建立新模型。
- [x] **步驟 2：配置本地連接環境變數與啟動** <!-- id: 1 -->
  - **原因**：設定環境變數 `ANTHROPIC_BASE_URL`、`ANTHROPIC_CUSTOM_MODEL_OPTION` 等，使 Claude CLI 能正確將 API 請求導向 Ollama 本地端並正常顯示。
- [x] **步驟 3：執行驗證與 Tool Calling 功能測試** <!-- id: 2 -->
  - **原因**：啟動 Claude Code 進行地端推論測試，驗證基本的對話與 Tool use（如檔案讀取/終端執行）是否運作正常。
- [x] **步驟 4：撰寫操作步驟文件並將計畫移至 `.archive` 歸檔** <!-- id: 3 -->
  - **原因**：整理出可重現的安裝與驗證步驟，並進行專案結構歸檔。
