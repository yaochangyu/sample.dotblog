# Claude 串接地端 Gemma 4 最佳實踐與避坑指南

本指南彙整了社群針對 **Claude (包含 Claude Code CLI 與 Claude Desktop)** 串接 Google 於 2026 年 4 月發布的 **Gemma 4** 地端開源模型之最佳實踐、優化設定與避坑指南。

---

## 1. Gemma 4 模型家族概述

Gemma 4 採用 Apache 2.0 授權，具備原生多模態（Multimodal）能力與 PLE（Per-Layer Embeddings）架構，適用於地端代理（Agentic）與程式碼生成任務：
- **Edge/Mobile 級別**：`gemma4:e2b` (Effective 2B) 與 `gemma4:e4b` (Effective 4B)。
- **筆電/工作站級別**：`gemma4:12b` (推薦筆電端 Agent 任務，無編碼器架構)。
- **伺服器/高端工作站**：`gemma4:26b` (MoE 架構) 與 `gemma4:31b` (Dense 高精度模型)。

---

## 2. 串接工具介紹與配置

由於 Claude Code 預設使用 Anthropic Messages API 格式，而地端 Ollama 主要是 OpenAI 相容格式，因此需要透過代理或 Ollama 的原生相容層進行串接。

### 方案 A：使用 LiteLLM 代理（最推薦、最靈活）

LiteLLM 作為代理伺服器（Proxy），能精準將 Anthropic API 格式轉換為 Ollama 格式，並支援強大的參數傳遞。

#### 1. 安裝與啟動
```bash
pip install litellm
# 啟動 LiteLLM 指向地端 Ollama 的 Gemma 4 模型
litellm --model ollama/gemma4:12b --port 4000
```

#### 2. 進階優化設定：使用 `litellm_config.yaml`
若要優化上下文長度與推論表現，建議使用配置文件啟動 LiteLLM：
```yaml
model_list:
  - model_name: claude-local
    litellm_params:
      model: ollama_chat/gemma4:12b # 使用 ollama_chat 以獲得更好的 Chat 格式相容性
      api_base: http://localhost:11434
      extra_body:
        options:
          num_ctx: 32768            # 強制將上下文擴展至 32k，避免 default 4k 崩潰
          temperature: 0.2
```
啟動指令：`litellm --config litellm_config.yaml --port 4000`

#### 3. 配置 Claude Code 環境變數
```bash
export ANTHROPIC_BASE_URL="http://localhost:4000"
export ANTHROPIC_API_KEY="temp-key" # 繞過 API Key 檢查
claude
```

---

### 方案 B：使用 Ollama 原生 Anthropic 相容 API

從 Ollama v0.14.0+ 開始，Ollama 提供了原生的 Anthropic Messages API 相容端點，您可以跳過 LiteLLM 直接串接。

#### 1. 使用 Modelfile 自訂上下文視窗
Ollama 預設的 `num_ctx` 僅有 4096，對於 Claude Code 這種會發送龐大 system prompt 的工具來說完全不夠。必須建立自訂模型：
1. 建立一個名名為 `Modelfile` 的檔案：
   ```dockerfile
   FROM gemma4:12b
   PARAMETER num_ctx 65536
   ```
2. 建立新模型：
   ```bash
   ollama create gemma4-64k -f Modelfile
   ```

#### 2. 直接指向 Ollama 啟動 Claude Code
設定自訂 `ANTHROPIC_BASE_URL` 時，為了使 Claude Code 能正確識別與顯示自訂的地端模型，建議透過自訂環境變數指定模型代碼與顯示名稱：

```bash
export ANTHROPIC_BASE_URL="http://localhost:11434"
export ANTHROPIC_API_KEY="ollama"
# 自訂模型設定（將自訂模型代號與顯示名稱傳入 Claude Code）
export ANTHROPIC_CUSTOM_MODEL_OPTION="gemma4-64k"
export ANTHROPIC_CUSTOM_MODEL_OPTION_NAME="Gemma 4 64k (Local)"
claude --model gemma4-64k
```

---

### 方案 C：使用 Claude Code Router (CCR)

`claude-code-router` 是一個專為 Claude Code 開發的社群開源中間件（Middleware），支援多 Provider 智慧路由與動態模型切換。

#### 1. 安裝
```bash
npm install -g @musistudio/claude-code-router
```

#### 2. 配置 `~/.claude-code-router/config.json`
```json
{
  "Providers": [
    {
      "name": "ollama",
      "api_base_url": "http://localhost:11434/v1/chat/completions",
      "api_key": "ollama",
      "models": ["gemma4:12b"]
    }
  ],
  "DefaultModel": "gemma4:12b"
}
```
*註：可透過 `npx -y @musistudio/claude-code-router ui` 開啟網頁介面進行配置。*

#### 3. 執行
CCR 會綁定在本地 `127.0.0.1:3456`，直接將 Claude Code 指向它即可：
```bash
export ANTHROPIC_BASE_URL="http://localhost:3456"
claude
```
在 Claude Code 終端中，您可以使用 `/model` 命令即時切換不同的後端模型。

---

### 方案 D：Claude Desktop 串接地端

目前 Anthropic 官方政策對於 **Claude Desktop** 使用自訂第三方 API 進行了較多限制（已逐步廢棄 `ollama launch claude-desktop` 等指令）。

**社群折衷替代方案**：
1. 在 Claude Desktop 點選 Troubleshooting 進入**開發者模式 (Developer Mode)**。
2. 將 Custom Gateway API URL 指向 LiteLLM（如 `http://localhost:4000`）或本地 Ollama 的相容端點。
3. **安全警告**：使用未受支援的 Proxy 繞過官方 API 有帳號受限之風險。若需要地端 Chat 體驗，強烈建議使用 **Open WebUI** 或 **LM Studio**；若需要地端程式輔助，則以 **Claude Code CLI** 為主。

---

## 3. 優化設定與避坑指南

### 🚨 避坑一：Tool Calling (工具調用) 失敗與格式錯誤
- **現象**：Claude Code 執行時需要頻繁讀寫檔案、執行終端指令，這極度仰賴模型的 Tool Calling 能力。若使用 `gemma4:e2b` 或 `gemma4:e4b`，常會因為參數量不足，無法正確輸出工具呼叫所需的 JSON 格式而卡死。
- **對策**：最低建議使用 **Gemma 4 12B**。若硬體許可（例如 32GB 以上統一記憶體的 Mac 或具備 24GB VRAM 以上的顯示卡），使用 **Gemma 4 26B (MoE)** 或 **31B** 能顯著提升 agent 任務的成功率。

### 🚨 避坑二：Thinking (思考推理) 模式相容性問題
- **現象**：較新版本的 Claude Code 會主動請求 `thinking` 功能（例如傳入 `reasoning_effort` 參數）。然而部分版本的 LiteLLM 或 Ollama 在接收到這些參數時會拋出 `400 Bad Request` 或 `invalid option`。
- **對策**：
  1. 確保 Ollama 升級至支援 thinking block 的最新版本（v0.9.0+）。
  2. 若 LiteLLM 代理報錯，可在 `litellm_config.yaml` 中關閉 reasoning 參數轉發，或在 Claude Code 中停用思考模式（有些版本可透過設定檔或指令關閉）。

### 🚨 避坑三：上下文視窗 (Context Window) 崩潰
- **現象**：Claude Code 載入程式專案時，經常會將整個目錄樹、多個檔案內容連同幾萬字的 System Prompt 一併發送。如果使用 Ollama 預設的 4096 Token 上下文，會導致嚴重的遺忘、迴圈輸出或報錯。
- **對策**：必須將 `num_ctx` 手動調大（**建議至少 32768**，Gemma 4 原生支援高達 256K 上下文）。
  - 使用 Modelfile 加入 `PARAMETER num_ctx 32768`。
  - 在 LiteLLM 設定檔中加入 `extra_body.options.num_ctx: 32768`。

### 🚨 避坑四：硬體 VRAM 溢出與推論極慢
- **現象**：當將 `num_ctx` 設大至 32k 或 64k 時，KV Cache 會佔用大量的 GPU 顯示記憶體（VRAM）。如果 VRAM 不足，Ollama 會自動將部分層（layers）或整個模型卸載至系統記憶體（RAM），導致推論速度（Tokens/s）驟降。
- **對策**：
  - 記憶體估算：運行 Gemma 4 12B (Q4_K_M) 約需 8GB VRAM。當 `num_ctx` 設為 32k 時，需要額外預留約 2-4GB VRAM 用於 KV Cache。
  - 若硬體吃緊，建議使用 E4B 或是選擇更低量化的 12B 版本（例如 Q4_0 或 Q3_K_L），以換取足夠的上下文空間。
