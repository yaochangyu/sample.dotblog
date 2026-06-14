# Claude Code + Gemma4 本地端優化配置報告

**日期**：2026-06-13  
**硬體**：RTX 4060 Laptop 8 GB VRAM / 32 核 CPU / 48 GB RAM  
**OS**：WSL2 (Ubuntu)

---

## 最終配置（已驗證可正常運作）

### Modelfile（`~/projects/Local-LLM/Modelfile`）

```dockerfile
FROM gemma4:e4b
PARAMETER num_ctx 32768
PARAMETER temperature 0.0
PARAMETER repeat_penalty 1.15
```

### litellm_config.yaml（`~/projects/Local-LLM/litellm_config.yaml`）

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

### empty-mcp.json（`~/projects/Local-LLM/empty-mcp.json`）

```json
{"mcpServers":{}}
```

---

## 使用方式

### 一鍵啟動（推薦）

```bash
cd ~/projects/Local-LLM
./run-claude.sh
```

### 手動啟動

```bash
# 步驟一：啟動本地環境
cd ~/projects/Local-LLM
./start-optimized-env.sh

# 步驟二：啟動 Claude Code（停用 MCP servers）
env ANTHROPIC_BASE_URL="http://localhost:4000" \
    ANTHROPIC_API_KEY="local-bypass" \
    claude --model gemma4-opt \
    --mcp-config ~/projects/Local-LLM/empty-mcp.json \
    --strict-mcp-config
```

---

## 核心問題診斷：為什麼需要 32K context？

Claude Code 啟動時，每次 API 請求都會夾帶所有 tool schemas：

| 來源 | 數量 | 估算 tokens |
|------|------|-------------|
| Built-in tools（Bash、Read、Edit 等） | 28 個 | **23,037 tokens** |
| System prompt（CLAUDE.md + memory） | — | 1,777 tokens |
| Messages（初始上下文） | 2 turn | 4,423 tokens |
| **總輸入 estimate** | | **~29,238 tokens** |
| MCP tools（GitHub、Gmail、Context7 等） | +31 個 | +~25,000 tokens（停用） |

**→ 若不停用 MCP：總輸入 ~54K tokens，遠超任何本地模型容量**  
**→ 停用 MCP 後：~29K tokens，num_ctx=32768 可容納**

---

## 優化前後比較

| 指標 | 初始狀態 | 最終優化 |
|------|---------|---------|
| 模型 | gemma4:e4b (8K ctx) | gemma4:e4b (32K ctx) |
| MCP servers | 啟用（59 tools / ~54K tokens） | **停用（28 tools / ~29K tokens）** |
| 生成速度 | 16.72 tok/s | **61.4 tok/s** |
| VRAM 用量 | 3.3 GB | **4.2 GB（含 32K KV cache）** |
| thinking block | 有（+43% tokens） | **無（think: false）** |
| repeat_penalty | 無 | **1.15（防重複迴圈）** |
| 實際可運作 | ✗ 重複迴圈 / 截斷 | **✓ 正常回應** |

---

## 各配置項說明

| 項目 | 設定值 | 原因 |
|------|--------|------|
| 模型 | `gemma4:e4b` | MoE 架構，8 GB VRAM 最佳選擇。12B dense 模型 + 32K KV cache = OOM |
| `num_ctx` | `32768` | Claude Code 28 built-in tools 本身就佔 ~23K tokens；低於 32K 必失敗 |
| `repeat_penalty` | `1.15` | 必要。無此設定會觸發「您 I I I」重複迴圈 |
| `temperature` | `0.0` | 工具呼叫任務使用確定性輸出 |
| `think: false` | extra_body | 關閉 gemma4 原生 thinking，節省 ~43% tokens |
| `drop_params: true` | litellm_settings | 過濾 Anthropic 專屬參數（如 `reasoning_effort`），避免 400 錯誤 |
| `--strict-mcp-config` | claude 參數 | 配合 empty-mcp.json，停用所有 MCP servers，tool 數 59 → 28 |
| `OLLAMA_FLASH_ATTENTION=1` | env var | 降低 KV cache VRAM 佔用，加速 prefill |

---

## 已知限制

- **對話長度有限**：初始請求 ~29K tokens，num_ctx=32768 僅剩 ~3.5K tokens 給回應；隨對話增長，context 很快滿溢。
- **無 MCP tools**：停用後無法使用 GitHub、Gmail、Context7 等工具；是 8 GB VRAM 的硬體限制。
- **12B 模型在此硬體不可行**：12b-qat (5.4 GB) + 32K KV cache (~3.5 GB) > 8 GB VRAM，會觸發 CPU offload（312 秒/次）。
- **gemma4:12b Q3 量化版**：亦已實測，CPU offload 問題相同，不建議。

---

## 失敗記錄（避免重踏）

| 嘗試 | 失敗原因 |
|------|---------|
| `gemma4:12b` dense | VRAM 不足，CPU offload → 312 秒/次 |
| `gemma4:12b-it-qat` + 32K | 5.4 GB model + 3.5 GB KV > 8 GB |
| `num_ctx=8192` + 任何模型 | 29K token input 遠超 8K，截斷後重複迴圈 |
| `--mcp-config '{}'` | 字串格式無效，MCP 仍載入（需用檔案路徑） |
| Modelfile `PARAMETER think false` | 無效 Modelfile 參數，需用 litellm extra_body |
| 移除 `repeat_penalty` | 觸發「您 I I I」重複迴圈 |
| `num_predict` 設定 | 覆蓋 API max_tokens，輸出截斷 |
