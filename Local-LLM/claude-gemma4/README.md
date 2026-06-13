# Local-LLM：Claude Code 串接地端 Gemma 4 實驗

在 RTX 4060 Laptop（8 GB VRAM）上，透過 **Ollama + LiteLLM** 讓 Claude Code CLI 改用本地 `gemma4:e4b` 模型推論的完整實驗紀錄。

---

## 硬體環境

| 項目 | 規格 |
|------|------|
| GPU | NVIDIA RTX 4060 Laptop 8 GB VRAM |
| RAM | 48 GB |
| OS | WSL2 (Ubuntu) |

---

## 架構

```
Claude Code CLI
      │  Anthropic Messages API
      ▼
LiteLLM Proxy :4000          ← 格式轉換（Anthropic → Ollama）
      │  Ollama Chat API
      ▼
Ollama :11435
      │
      ▼
gemma4-opt（FROM gemma4:e4b, num_ctx=32768）
```

**為什麼需要 LiteLLM？**  
Claude Code 送出的請求包含 Anthropic 專屬參數（`reasoning_effort` 等），Ollama 無法直接接受；LiteLLM 負責過濾並轉換格式。

---

## 快速啟動

```bash
cd ~/projects/Local-LLM
./run-claude.sh
```

腳本會自動：
1. 啟動 Ollama（port 11435）
2. 建立 `gemma4-opt` 模型（若不存在）
3. 啟動 LiteLLM proxy（port 4000）
4. 以正確參數啟動 Claude Code

---

## 檔案說明

| 檔案 | 用途 |
|------|------|
| `run-claude.sh` | 一鍵啟動腳本（主要入口） |
| `start-optimized-env.sh` | 環境啟動層（Ollama + LiteLLM） |
| `Modelfile` | 定義 gemma4-opt 模型參數 |
| `litellm_config.yaml` | LiteLLM proxy 設定 |
| `empty-mcp.json` | 停用 MCP servers 用的空設定檔 |
| `capture_proxy.py` | HTTP logging proxy，診斷 token 用量 |
| `optimization-report.md` | 完整優化過程與數據 |
| `claude-gemma4-best-practices.md` | 串接方式比較（LiteLLM/Ollama/CCR） |
| `ollama_optimization_steps.md` | 8 GB VRAM 軟體調校指南 |
| `gemma4_best_practices_experiment_report.md` | 12B vs e4b 效能對比實驗 |

---

## 最終配置

### Modelfile

```dockerfile
FROM gemma4:e4b
PARAMETER num_ctx 32768
PARAMETER temperature 0.0
PARAMETER repeat_penalty 1.15
```

### litellm_config.yaml

```yaml
model_list:
  - model_name: gemma4-opt
    litellm_params:
      model: ollama_chat/gemma4-opt
      api_base: http://localhost:11435
      extra_body:
        think: false
        options:
          num_ctx: 32768

litellm_settings:
  drop_params: true
```

---

## 關鍵發現

### Claude Code 的 token 結構

每次請求的固定輸入成本：

| 來源 | tokens |
|------|--------|
| 28 個 built-in tool schemas | ~23,037 |
| System prompt（CLAUDE.md + memory） | ~1,777 |
| 初始 messages（上下文載入） | ~4,423 |
| **合計** | **~29,238** |

→ `num_ctx` 必須 ≥ 32768，低於此值必然截斷 → 重複迴圈。

### 為什麼停用 MCP servers？

預設啟用 MCP 時（GitHub、Gmail、Context7 等），tool 數從 28 增加至 59，輸入 tokens 從 ~29K 飆升至 ~54K，完全超出任何能在 8 GB VRAM 運行的本地模型上限。

透過 `--strict-mcp-config` + `empty-mcp.json` 將 MCP servers 全部停用。

### VRAM 實測

| 配置 | VRAM 用量 | 速度 |
|------|-----------|------|
| gemma4:12b + 32K | >8 GB（CPU offload） | ~8 tok/s |
| gemma4:e4b + 8K | 3.3 GB | 62 tok/s |
| **gemma4:e4b + 32K（現行）** | **4.2 GB** | **61 tok/s** |

---

## 已知限制

- **對話長度有限**：初始請求 ~29K tokens，32K context 僅剩 ~3.5K 給回應；隨對話增長 context 快速填滿。
- **無 MCP tools**：GitHub、Gmail、Context7 等全部停用；是 8 GB VRAM 的硬體限制，非設計選擇。
- **12B 模型不可行**：5.4 GB 模型 + 3.5 GB KV cache > 8 GB，必然觸發 CPU offload。
