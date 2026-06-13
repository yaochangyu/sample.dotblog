# Gemma 4 最佳實踐實驗與實測計畫

本計畫旨在依據 `claude-gemma4-best-practices.md` 的指導原則（使用 Gemma 4 12B 模型並設定 32k 上下文長度），於 `/home/yao/projects/Local-LLM` 進行地端 LLM 實驗，並於當前目錄產出實測報告。

- [x] **步驟 1：於實驗目錄建立配置 32k 上下文的 Gemma 4 12B 自訂模型** <!-- id: 0 -->
  - **原因**：依據通用最佳實踐，為了獲得穩定的 Tool Calling 能力與防止上下文視窗崩潰，需在 Modelfile 中使用 `gemma4:12b` 並設定 `num_ctx 32768`。
- [x] **步驟 2：配置並啟動獨立 Ollama 服務與 LiteLLM 代理** <!-- id: 1 -->
  - **原因**：在獨立連接埠運行 Ollama，並透過 LiteLLM 轉譯以防範 Claude Code 送出不相容參數，同時在 `litellm_config.yaml` 限制上下文為 32k。
- [x] **步驟 3：指派 Subagent 執行 Claude Code 地端連線實測與效能監控** <!-- id: 2 -->
  - **原因**：在實驗路徑實際連線地端模型，測試專案程式碼審查等 Agentic 任務，並記錄推論速度（tokens/s）與 VRAM/RAM 資源佔用。
- [x] **步驟 4：產出實測報告至當前目錄並同步所有變更檔案** <!-- id: 3 -->
  - **原因**：將實測報告寫入當前目錄的 `gemma4_best_practices_experiment_report.md`，並將所有測試成果檔案與報告同步至 `/home/yao/projects/Local-LLM/`，同時更新專案樹 `@tree.md`。
- [x] **步驟 5：將本計畫檔移至 `.archive/` 進行歸檔** <!-- id: 4 -->
  - **原因**：完成所有實驗與報告輸出，對計畫進行歸檔封存。
