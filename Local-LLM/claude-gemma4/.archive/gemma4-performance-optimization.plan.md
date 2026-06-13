# Gemma4 Performance Optimization Plan

## 目標
提升 Claude + gemma4-opt 的推論速度，從目前 16.72 tok/s 優化至接近 20+ tok/s。

## 環境
- 實驗路徑：`~/projects/Local-LLM`
- Ollama port：11435，LiteLLM port：4000
- 硬體：RTX 4060 Laptop 8GB VRAM（剩餘 ~3.8 GB）

---

## 步驟

- [x] **步驟 1：關閉 thinking 模式**
  - **為什麼**：LiteLLM 把 thinking 參數轉給 Ollama，觸發推理模式，多產生 43% 額外 token。加入 `drop_params: true` + `think: false` 關閉。
  - **修改檔案**：`~/projects/Local-LLM/litellm_config.yaml`
  - **結果**：thinking block 確認關閉，output_tokens 從 1389 降至 706（減少 49%）。

- [x] **步驟 2：提升 `num_ctx` 至 16384**
  - **為什麼**：原 8192 對 Claude Code 載入中型專案偏小。VRAM 剩餘空間足夠。
  - **修改檔案**：`~/projects/Local-LLM/Modelfile`、`~/projects/Local-LLM/litellm_config.yaml`
  - **結果**：gemma4-opt 重建完成，num_ctx 16384 已生效。

- [x] **步驟 3：移除 `num_predict` 限制**
  - **為什麼**：原 `num_predict 2048` 會截斷 agent 長輸出。
  - **修改檔案**：`~/projects/Local-LLM/Modelfile`（移除該行）
  - **結果**：已移除，改由 LiteLLM `max_tokens` 控制。

- [x] **步驟 4：確認 WSL2 `.wslconfig` CPU 核心數**
  - **結果**：未限制 processors，已使用全部 32 核，無需修改。

- [x] **步驟 5：基準測試確認優化效果**

| 指標 | 優化前 | 優化後 | 提升幅度 |
|------|--------|--------|----------|
| Ollama 生成速度 | 16.72 tok/s | **61.00 tok/s** | +265% |
| LiteLLM 端到端 | 14.52 tok/s | **53.3 tok/s** | +267% |
| thinking block | 有（43% 浪費） | **無** | 消除 |
| num_ctx | 8192 | **16384** | 2x |
| output_tokens（同 prompt） | 1389 | **706** | -49% |

## 根本原因備註
`gemma4:12b` 在 8 GB VRAM 不適合（超出 VRAM 導致 CPU 卸載，推論 312 秒）。
`gemma4:e4b`（MoE 架構，VRAM 實際僅佔 3.3 GB）更適合此硬體。
