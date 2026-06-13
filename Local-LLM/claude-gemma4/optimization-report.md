# Claude + Gemma4 本地端優化配置報告

**日期**：2026-06-13  
**硬體**：RTX 4060 Laptop 8 GB VRAM / 32 核 CPU / 48 GB RAM  
**OS**：WSL2 (Ubuntu)

---

## 最終配置

### Modelfile（`~/projects/Local-LLM/Modelfile`）

```dockerfile
FROM gemma4:e4b
PARAMETER num_ctx 16384
PARAMETER temperature 0.0
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
          num_ctx: 16384

litellm_settings:
  drop_params: true
```

---

## 使用方式

### 步驟一：啟動本地環境

```bash
cd ~/projects/Local-LLM
./start-optimized-env.sh
```

### 步驟二：啟動 Claude Code 串接本地 gemma4

```bash
export ANTHROPIC_BASE_URL="http://localhost:4000"
export ANTHROPIC_API_KEY="local-bypass"
claude --model gemma4-opt
```

或一行執行：

```bash
ANTHROPIC_BASE_URL="http://localhost:4000" ANTHROPIC_API_KEY="local-bypass" claude --model gemma4-opt
```

### 步驟三：確認服務正常（可選）

```bash
# 確認 Ollama 模型已載入
curl -s http://localhost:11435/api/ps | python3 -m json.tool

# 確認 LiteLLM 可接受請求
curl -s http://localhost:4000/v1/messages \
  -H "Content-Type: application/json" \
  -H "x-api-key: local-bypass" \
  -H "anthropic-version: 2023-06-01" \
  -d '{"model":"gemma4-opt","max_tokens":50,"messages":[{"role":"user","content":"hi"}]}'
```

---

## 優化前後比較

| 指標 | 優化前 | 優化後 |
|------|--------|--------|
| 生成速度（Ollama） | 16.72 tok/s | **60.18 tok/s**（+260%） |
| Prefill 速度 | 94.37 tok/s | **119.96 tok/s**（+27%） |
| LiteLLM 端到端 | 14.52 tok/s | **53.3 tok/s**（+267%） |
| thinking block | 有（佔 43% token） | **無** |
| num_ctx | 8192 | **16384** |
| 模型 VRAM 佔用 | 3.3 GB | **2.9 GB** |

---

## 優化項目說明

| 項目 | 設定 | 說明 |
|------|------|------|
| 模型選擇 | `gemma4:e4b` | MoE 架構，8 GB VRAM 硬體最佳選擇；12B dense 模型會因 VRAM 不足觸發 CPU 卸載（實測 312 秒/次） |
| `num_ctx` | 16384 | 原 8192 對 Claude Code 的 system prompt 偏小；16384 VRAM 仍可容納 |
| `think: false` | extra_body | gemma4 原生 thinking 模式，透過 LiteLLM extra_body 關閉 |
| `drop_params: true` | litellm_settings | 過濾 Claude Code 傳入的 Anthropic 專屬參數（如 `reasoning_effort`），避免 400 錯誤 |
| `temperature: 0.0` | Modelfile | 工具呼叫任務使用確定性輸出，減少 token 浪費 |
| Flash Attention | `OLLAMA_FLASH_ATTENTION=1` | start-optimized-env.sh 已設定，降低 VRAM 佔用並加速 prefill |

---

## 注意事項

- `gemma4:12b` 在此硬體（8 GB VRAM）**不建議使用**，模型本身 7.6 GB + KV Cache 超出 VRAM，觸發 CPU 卸載後推論速度極慢。
- `num_ctx` 不宜再調高；32768 已導致 12B 模型 CPU 卸載，e4b 在 16384 仍需監控 VRAM。
- LiteLLM 重啟後需約 8 秒啟動時間，才能接受請求。
