# [本地 AI] 在 8GB VRAM 筆電讓 Claude Code 串接 Gemma 4 E4B：Ollama + LiteLLM 完整踩坑紀錄

最近 Google 釋出了 Gemma 4 系列，其中 `gemma4:e4b` 是專為邊緣裝置設計的版本，4.5B 有效參數，理論上筆電可以跑。

我想說趁這個機會，把 Claude Code 的推論後端換成本地端，省點 API token XDD

結果發現「跑得起來」跟「能正常工作」是兩件完全不同的事。這篇就是把整個踩坑過程記下來，包含架構設計、VRAM 預算計算、為什麼一定要加 LiteLLM、以及 Claude Code 的 token 結構分析。

---

## 開發環境

- OS：Windows 11 / Ubuntu（WSL2）
- GPU：NVIDIA RTX 4060 Laptop 8 GB VRAM
- RAM：48 GB
- CUDA：12.5（驅動 556.29）
- Ollama：latest（v0.30.8+）
- LiteLLM：latest（`litellm[proxy]`）
- Claude Code：latest
- Model：`gemma4:e4b`（基底）→ `gemma4-opt`（自訂）

---

## Gemma 4 E4B 是什麼

先簡單說一下這顆模型：

- **有效參數**：4.5B（MoE 架構，E 代表 Effective，實際推論只用 4.5B）
- **模型大小**：約 3.3 GB（Q4 量化）
- **Context 長度**：原生支援 128K tokens
- **授權**：Apache License 2.0
- **支援輸入**：文字、圖片、音訊

重點是它是 Mixture of Experts 架構，所以實際佔用 VRAM 比全參數 4.5B 還少很多，這是為什麼在 8GB VRAM 可以跑的原因。

---

## Step 1：安裝 Ollama

前往 [https://ollama.com/download](https://ollama.com/download) 下載對應版本，支援 macOS、Windows、Linux。

確認 Ollama 有在跑：

```bash
ollama --version
```

---

## Step 2：拉取 gemma4:e4b 模型

下面這個指令會拉取模型並直接開對話，第一次跑會下載約 3.3 GB：

```bash
ollama run gemma4:e4b
```

拉完後在終端機跟它說幾句確認正常，按 `Ctrl+D` 離開。

到這裡看起來一切美好，然後你興沖沖地啟動 Claude Code，然後就噴掉了啦!!!

---

## 為什麼直接串接會壞

我一開始的做法是直接把 Claude Code 指向 Ollama：

```bash
export ANTHROPIC_BASE_URL="http://localhost:11434"
export ANTHROPIC_API_KEY="ollama"
claude --model gemma4:e4b
```

結果不是卡死就是一直重複輸出一樣的字，或者 400 Bad Request。

問題出在兩個地方：

**問題一：Claude Code 的 token 用量超乎想像**

Claude Code 每次 API 請求會把所有 tool schemas 一起送過去：

| 來源 | tokens |
|------|--------|
| 28 個 built-in tool schemas（Bash、Read、Edit 等） | ~23,037 |
| System prompt（CLAUDE.md + memory） | ~1,777 |
| 初始 messages（上下文載入） | ~4,423 |
| **合計** | **~29,238** |

Ollama 預設 `num_ctx` 是 4096，有時甚至更低。輸入 29K tokens 塞進去，context 截斷之後模型就開始鬼打牆了。

更慘的是，如果還有開 MCP servers（GitHub、Gmail、Context7 等），tool 數從 28 跳到 59，輸入 tokens 直接飆到 **~54K**，完全沒有任何本地模型能吃得下。

**問題二：Anthropic 專屬參數 Ollama 吃不消**

Claude Code 發出的請求裡面有 Anthropic 特有的欄位，例如 `reasoning_effort`、部分 Beta 標頭。Ollama 的 Messages API 看到這些東西就 400 了。

---

## 架構設計：為什麼一定要 LiteLLM

解法是加一層 LiteLLM Proxy 在中間：

```
Claude Code CLI
      │  Anthropic Messages API（含 Anthropic 專屬欄位）
      ▼
LiteLLM Proxy :4000   ← 格式轉換 + 參數過濾 + drop_params
      │  Ollama Chat API（乾淨格式）
      ▼
Ollama :11435
      │
      ▼
gemma4-opt（FROM gemma4:e4b, num_ctx=32768）
```

Ollama 負責推論算力，LiteLLM 負責格式轉換與邊界防護。兩個角色的差異：

| 比較維度 | Ollama | LiteLLM |
|:---|:---|:---|
| **核心職責** | 載入模型、GPU 推論 | API 格式轉譯、參數過濾 |
| **Anthropic 參數相容性** | 有限，遇到未知欄位直接 400 | 完美，內建轉換層 + `drop_params` |
| **num_ctx 控制** | 只能透過 Modelfile 靜態設定 | 可在 config 動態覆寫 |
| **效能損耗** | 無 | 微秒級 HTTP 延遲，可忽略 |

---

## Step 3：VRAM 預算計算

8GB VRAM 扣掉 Windows 桌面 UI 佔用，實際可用大概只有 **6.5 ~ 7.0 GB**。

$$\text{總 VRAM 需求} = \text{模型權重} + \text{KV 快取} + \text{CUDA 執行期開銷}$$

- `gemma4:e4b` 模型權重：約 3.3 GB
- `num_ctx=32768` 的 KV 快取（FP16）：約 0.7 GB
- CUDA 執行期：約 0.2 GB
- **合計：約 4.2 GB** ← 有空間

反過來說，如果你想換 `gemma4:12b`（7.6 GB 模型權重），加上 32k KV 快取，就算 Ollama 塞得進去，也必然觸發 CPU Offloading，推論速度從 61 tok/s 暴跌到 **8 tok/s**。實測 341 秒才跑出一個回覆，Claude Code 的 Agent 迴圈完全沒法用。

**8GB 筆電的現實選擇就是 e4b。**

---

## Step 4：WSL2 與 NVIDIA 設定（避免被 OOM 殺掉）

在 WSL2 環境跑 GPU 工作負載，建議先把這兩個設定做好：

**`C:\Users\<YourUsername>\.wslconfig`**（防止 WSL2 隨機 OOM 殺掉 Ollama）

```ini
[wsl2]
memory=24GB
swap=16GB
processors=8
guiApplications=false
```

設定後在 PowerShell 執行 `wsl --shutdown` 重啟 WSL2。

**NVIDIA 控制面板**

進入「管理 3D 設定」，將「CUDA - 系統記憶體後備原則」改為「偏好系統記憶體不後備」，防止 CUDA 偷偷把 VRAM 溢出部分丟到系統 RAM。

---

## Step 5：建立最終設定檔

共三個檔案，放在專案的 `script/` 目錄下。

**Modelfile**（調整 e4b 的推論參數）

```dockerfile
FROM gemma4:e4b
PARAMETER num_ctx 32768
PARAMETER temperature 0.0
PARAMETER repeat_penalty 1.15
```

- `num_ctx 32768`：必須 ≥ 29K，否則 Claude Code 的 tool schemas 必定被截斷
- `temperature 0.0`：確保 Tool Calling 的 JSON 格式輸出穩定
- `repeat_penalty 1.15`：必要，沒這個模型會進入「您 I I I I」重複迴圈

**litellm_config.yaml**

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

- `think: false`：關閉 Gemma 4 原生 thinking block，省下 ~43% tokens
- `drop_params: true`：過濾 Anthropic 專屬欄位，避免送到 Ollama 時 400

**empty-mcp.json**（停用所有 MCP servers）

```json
{"mcpServers":{}}
```

---

## Step 6：建立自訂模型與啟動服務

先在獨立 port 啟動 Ollama（避免跟系統預設的 11434 衝突）：

```bash
export OLLAMA_HOST="127.0.0.1:11435"
export OLLAMA_NUM_PARALLEL=1
export OLLAMA_MAX_LOADED_MODELS=1
export OLLAMA_FLASH_ATTENTION=1
ollama serve &
```

建立套用自訂參數的模型：

```bash
export OLLAMA_HOST="127.0.0.1:11435"
ollama create gemma4-opt -f script/Modelfile
```

啟動 LiteLLM Proxy：

```bash
pip install 'litellm[proxy]' --break-system-packages
litellm --config script/litellm_config.yaml --port 4000 &
```

---

## Step 7：啟動 Claude Code（停用 MCP）

正式啟動指令，停用 MCP servers 讓 tool 數從 59 降回 28：

```bash
export ANTHROPIC_BASE_URL="http://localhost:4000"
export ANTHROPIC_API_KEY="local-bypass"

claude --model gemma4-opt \
       --mcp-config script/empty-mcp.json \
       --strict-mcp-config
```

---

## 腳本整理（懶人用）

上面所有步驟拆成三支腳本，放在 `script/` 目錄下：

```
script/
├── install.sh            # 安裝 Ollama、拉取模型、安裝 LiteLLM
├── start-optimized-env.sh  # 啟動 Ollama + 建立 gemma4-opt + 啟動 LiteLLM
├── run-claude.sh         # 呼叫 start-optimized-env.sh 後直接啟動 Claude Code
├── Modelfile
├── litellm_config.yaml
└── empty-mcp.json
```

### install.sh（第一次執行一次就好）

```bash
#!/bin/bash

echo "=========================================================="
echo "         Gemma 4 E4B + LiteLLM 環境安裝腳本"
echo "=========================================================="

# 1. 安裝 Ollama
if ! command -v ollama &>/dev/null; then
    echo "[1/3] 安裝 Ollama..."
    curl -fsSL https://ollama.com/install.sh | sh
else
    echo "[1/3] Ollama 已安裝，跳過。"
fi

# 2. 拉取 gemma4:e4b 模型
echo "[2/3] 拉取 gemma4:e4b 模型（約 3.3 GB，首次執行需等待）..."
ollama pull gemma4:e4b

# 3. 安裝 LiteLLM Proxy
if ! command -v litellm &>/dev/null; then
    echo "[3/3] 安裝 LiteLLM Proxy..."
    pip install 'litellm[proxy]' --break-system-packages
else
    echo "[3/3] LiteLLM 已安裝，跳過。"
fi

echo "----------------------------------------------------------"
echo "✨ 安裝完成！接下來執行："
echo "   bash script/start-optimized-env.sh"
echo "----------------------------------------------------------"
```

### start-optimized-env.sh（每次開機執行一次）

```bash
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export OLLAMA_HOST="127.0.0.1:11435"
export OLLAMA_NUM_PARALLEL=1
export OLLAMA_MAX_LOADED_MODELS=1
export OLLAMA_FLASH_ATTENTION=1

echo "=========================================================="
echo "         地端 Ollama + LiteLLM 一鍵優化啟動腳本"
echo "=========================================================="

# 1. 啟動獨立 Ollama 服務
if ! lsof -i :11435 >/dev/null 2>&1; then
    echo "[1/3] 正在背景啟動獨立 Ollama 服務 (Port: 11435)..."
    ollama serve > "$SCRIPT_DIR/ollama_11435.log" 2>&1 &
    for i in {1..10}; do
        if curl -s http://127.0.0.1:11435 >/dev/null; then
            break
        fi
        sleep 1
    done
else
    echo "[1/3] Ollama 服務已在 Port 11435 運行中。"
fi

# 2. 檢查並自動建立 gemma4-opt 模型
if ! OLLAMA_HOST="127.0.0.1:11435" ollama list | grep -q "gemma4-opt"; then
    echo "[2/3] 地端模型 gemma4-opt 不存在，正在自動建立..."
    if [ -f "$SCRIPT_DIR/Modelfile" ]; then
        OLLAMA_HOST="127.0.0.1:11435" ollama create gemma4-opt -f "$SCRIPT_DIR/Modelfile"
    else
        echo "錯誤：找不到 Modelfile，無法建立優化模型。"
        exit 1
    fi
else
    echo "[2/3] 地端優化模型 gemma4-opt 已就緒。"
fi

# 3. 啟動 LiteLLM 服務
if ! lsof -i :4000 >/dev/null 2>&1; then
    echo "[3/3] 正在背景啟動 LiteLLM Proxy (Port: 4000)..."
    if [ -f "$SCRIPT_DIR/litellm_config.yaml" ]; then
        litellm --config "$SCRIPT_DIR/litellm_config.yaml" --port 4000 > "$SCRIPT_DIR/litellm.log" 2>&1 &
        sleep 3
    else
        echo "錯誤：找不到 litellm_config.yaml，無法啟動 LiteLLM。"
        exit 1
    fi
else
    echo "[3/3] LiteLLM 代理服務已在 Port 4000 運行中。"
fi

echo "----------------------------------------------------------"
echo "✨ 所有地端優化服務已成功啟動！"
echo "請執行以下指令啟動 Claude Code："
echo ""
echo "   bash script/run-claude.sh"
echo "----------------------------------------------------------"
```

### run-claude.sh（每次要用 Claude Code 時執行）

```bash
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

bash "$SCRIPT_DIR/start-optimized-env.sh"

# empty MCP config 停用所有 MCP servers，讓 tool 數從 59 降至 28
# 28 tools × ~3.3K chars = 23K tokens + 1.8K system + 4.4K msg = ~29K，恰好塞進 num_ctx=32768
env ANTHROPIC_BASE_URL="http://localhost:4000" \
    ANTHROPIC_API_KEY="local-bypass" \
    claude --model gemma4-opt \
    --mcp-config "$SCRIPT_DIR/empty-mcp.json" \
    --strict-mcp-config
```

三支腳本的使用時機：

| 腳本 | 執行時機 |
|------|----------|
| `install.sh` | 第一次設定環境，只跑一次 |
| `start-optimized-env.sh` | 每次重開機後，啟動背景服務 |
| `run-claude.sh` | 每次要開始用 Claude Code 時 |

---

## 實測結果

| 指標 | 數據 |
|------|------|
| VRAM 佔用 | 4.2 GB（穩定，無 CPU Offload） |
| 生成速度 | **61.4 tok/s** |
| 輸入 token 估計 | ~29,238（28 tools + system prompt + messages） |
| repeat_penalty 效果 | 無重複迴圈，輸出正常收斂 |
| think block 關閉後 | token 消耗降低約 43% |

和幾種配置的對比：

| 配置 | VRAM | 速度 | 可用 |
|------|------|------|------|
| gemma4:e4b + 8K ctx | 3.3 GB | 62 tok/s | ✗ tool schemas 截斷 |
| gemma4:12b + 32K ctx | >8 GB（CPU offload） | 8 tok/s | ✗ 等到天荒地老 |
| **gemma4-opt + 32K ctx（現行）** | **4.2 GB** | **61 tok/s** | **✓** |

---

## 避坑整理

**坑一：Tool Calling 格式錯誤 / 卡死**

- 現象：Claude Code 執行工具呼叫時，模型輸出的 JSON 格式亂掉或輸出空字串
- 對策：`temperature 0.0` + `repeat_penalty 1.15` 缺一不可

**坑二：Thinking block 導致 400 Bad Request**

- 現象：新版 Claude Code 會送 `reasoning_effort` 參數，Ollama 看到就噴錯
- 對策：`litellm_settings.drop_params: true` + `extra_body.think: false`

**坑三：Context Window 截斷 → 重複迴圈**

- 現象：輸出一直重複同一句話，像壞掉的唱片
- 對策：`num_ctx` 必須 ≥ 32768，低於這個值必然截斷

**坑四：Modelfile 裡寫 `PARAMETER think false` 無效**

- 現象：以為在 Modelfile 設了就能關 thinking，結果完全沒用
- 對策：think 參數只能透過 LiteLLM 的 `extra_body` 傳，不是 Modelfile 參數

**坑五：MCP servers 沒停用**

- 現象：開了 GitHub、Gmail 等 MCP 之後，tool 數從 28 跳到 59，輸入 tokens 飆到 54K
- 對策：`--strict-mcp-config` + `empty-mcp.json` 把 MCP 全停掉

**坑六：`--mcp-config '{}'` 字串格式無效**

- 現象：以為可以直接傳 JSON 字串，結果 Claude Code 沒吃到，MCP 還是載入了
- 對策：一定要用檔案路徑，不能用 inline 字串

---

## 已知限制

- **對話長度有限**：初始請求就吃掉 ~29K，32K context 只剩 ~3.5K 給回應；隨對話增長，context 很快滿溢，建議短對話、高頻啟動新 session
- **無 MCP tools**：GitHub、Gmail、Context7 等全停了，這是 8 GB VRAM 的硬體限制，不是 bug
- **12B 在這台跑不動**：5.4 GB 模型 + 3.5 GB KV cache > 8 GB，CPU Offload 的速度沒法用

---

## 心得

- 本地模型跑 Claude Code 最大的坑不是模型能力，是 **token 預算**。Claude Code 的工具描述本身就吃掉了 23K tokens，遠超一般人對「本地模型」的 context 預期。
- LiteLLM 的 `drop_params: true` 是整個方案的關鍵；沒有它，Anthropic 的 Beta 標頭會讓 Ollama 一直 400。
- `repeat_penalty 1.15` 跟 `temperature 0.0` 對工具呼叫的穩定度影響非常大，不要省。
- 速度上 61 tok/s 其實蠻夠用的，比 Claude API 慢一點，但可以本地跑沒有延遲費。

---

若有謬誤,煩請告知,新手發帖請多包涵
