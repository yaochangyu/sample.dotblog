# Ollama 8GB VRAM 軟體優化實作與驗證計畫

本計畫旨在實作地端 Ollama 與 Claude Code 串接在 8GB VRAM 限制下的軟體優化配置，並進行速度與穩定性驗證。

- [x] **步驟 1：建立優化參數的自訂 Modelfile-opt 並建立 gemma4-opt 模型** <!-- id: 0 -->
  - **原因**：為了在 8GB VRAM 下避免 KV Cache 溢出導致的 CPU 卸載，需建立一個指定 `num_ctx 8192`、`temperature 0.0` 且基於 `q3_K_M` 的自訂模型。
- [x] **步驟 2：配置環境變數啟動腳本與 LiteLLM 代理設定** <!-- id: 1 -->
  - **原因**：設定環境變數 `OLLAMA_NUM_PARALLEL=1` 以防止並行 KV Cache 溢出，並透過 `litellm_config.yaml` 將 Claude Code API 請求導向優化後的 `gemma4-opt`。
- [x] **步驟 3：啟動代理服務並進行 Claude Code 整合驗證** <!-- id: 2 -->
  - **原因**：在背景啟動優化後的 Ollama 服務與 LiteLLM，並透過 Claude CLI 指向本地代理，驗證模型對話與 Tool use 運作是否正常。
- [x] **步驟 4：整理優化後的操作步驟與指標，將本計畫移至 `.archive` 歸檔** <!-- id: 3 -->
  - **原因**：紀錄最終實作的關鍵步驟與成果，並完成專案計畫的歸檔。
