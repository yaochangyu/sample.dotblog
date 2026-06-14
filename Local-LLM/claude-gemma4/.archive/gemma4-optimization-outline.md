# 地端 Gemma 4 + Claude Code 極致優化與避坑指南 — 草案

- **目的**：分享如何在 8GB VRAM 硬體限制下，透過軟體調校與雙層架構，使 Claude Code 與地端 Gemma 4 穩定且高速運行。
- **受眾**：技術論壇 / Demo 觀眾（DevOps、Developer Experience 工程師、AI 開發者）
- **風格**：Dark（高對比暗色，著重代碼與實測數據）
- **規模**：9 頁 / 約 15 分鐘
- **講者備註**：需要
- **資產**：無（純文字與代碼）

---

## 1. Cover — 地端 Gemma 4 + Claude Code 極致優化與避坑指南
- **版型**：Cover
- **重點**：
  - 主標題：地端 Gemma 4 + Claude Code 極致優化與避坑指南
  - 副標題：8GB VRAM 的極限生存戰：從重複迴圈到 61.4 tok/s 原生速度
  - 講者：DevOps / DX 團隊
- **備註**：開場直接拋出痛點：8GB VRAM（如 RTX 4060 Laptop）是否能流暢跑 Agent 任務？今天帶走一套已驗證、可重現的優化架構。

## 2. Stat — 8GB VRAM 的殘酷邊界與黃金法則
- **版型**：Stat
- **重點**：
  - **6.5 GB**：8GB VRAM 扣除系統後實際可用空間
  - **1.5 GB**：在 8K 上下文（num_ctx）時 KV Cache 佔用空間
  - **-90%**：一旦觸發 CPU 混合推論時的速度跌幅（從 25 tok/s 暴跌至 2 tok/s）
- **備註**：強調「黃金法則」：必須將整個模型權重與 KV Cache 完全限制在 VRAM 內。只要有任何一層溢出至系統 RAM，速度就會崩塌。

## 3. Comparison — Ollama vs. LiteLLM：心臟與防禦盾牌
- **版型**：Comparison
- **重點**：
  - **Ollama (推論引擎)**：
    - 提供「硬體推論算力」，負責載入模型權重與執行 GPU/CUDA 運算。
    - API 參數相容性有限，遇到 Anthropic Beta 參數容易崩潰。
  - **LiteLLM (API 閘道器)**：
    - 擔任「防禦盾牌」，翻譯 API 格式並過濾不相容參數。
    - 提供精細參數控制，強制攔截與限制 context 邊界。
- **備註**：說明為什麼我們需要雙層架構，而不是讓 Claude Code 直連 Ollama。

## 4. Activity — 診斷：為什麼啟動 Claude Code 就直接崩潰？
- **版型**：Activity
- **情境**：啟動 Claude Code 進行簡單的 Bug 修正或詢問。
- **任務**：估算第一次 API 請求（Prefill 階段）所送出的 Token 數量。
- **時間**：個人思考 2 分鐘。
- **產出**：理解 prefill 消耗結構。
- **討論問題**：
  - 內建 28 個工具的 schemas 就佔用了 23,037 tokens！加上 System Prompt 與歷史，初始輸入高達 **29K tokens**。
  - 如果你使用預設的 4K 或 8K 上下文，會發生什麼事？（答：直接截斷或陷入重複輸出迴圈）。
- **備註**：讓觀眾透過真實數據算帳，明白為什麼「32K 上下文」是地端執行的起跑線。

## 5. Code — 核心配置：防迴圈與減肥設定
- **版型**：Code
- **重點**：
  - 展示 Modelfile 與 litellm_config.yaml 的精華設定：
    ```dockerfile
    # Modelfile 關鍵參數
    FROM gemma4:e4b
    PARAMETER num_ctx 32768
    PARAMETER repeat_penalty 1.15
    PARAMETER temperature 0.0
    ```
    ```yaml
    # litellm_config.yaml 關鍵參數
    extra_body:
      think: false          # 停用原生 thinking，節省 43% tokens
    litellm_settings:
      drop_params: true     # 屏蔽不相容參數（如 reasoning_effort）
    ```
- **備註**：著重解釋 `repeat_penalty` 的防迴圈作用，以及停用 `think` 的 token 節省策略。

## 6. Workflow — 停用 MCP：讓工具集「精準瘦身」
- **版型**：Workflow
- **流程**：
  - **輸入**：預設啟用 MCP 導致的 59 個 tools（總計 ~54K tokens，超出 32K 限制）
  - **步驟 1**：建立一個空白的 `empty-mcp.json`
  - **步驟 2**：啟動時使用 `--mcp-config` 指向該檔案
  - **步驟 3**：加上 `--strict-mcp-config` 強制停用外掛
  - **輸出**：僅保留 28 個內建 tools（總計 ~29K tokens，完美塞入 32K 上下文）
- **備註**：這是在 8GB VRAM 限制下的必要妥協，把空間留給真正的對話。

## 7. Before/After — 軟體優化後的效能躍進
- **版型**：Before/After
- **重點**：
  - **優化前 (Unoptimized)**：
    - 模型與設定：gemma4:e4b (8K ctx) + 啟用 MCP
    - 狀態：✗ 截斷崩潰 / 重複迴圈，無法正常運作
    - 速度：極慢或無輸出
  - **優化後 (Optimized)**：
    - 模型與設定：gemma4:e4b (32K ctx) + 停用 MCP + 關閉 thinking
    - 狀態：✓ 正常回應，工具呼叫格式精準
    - 速度：**61.4 tokens/s** (全 GPU 推論)
- **備註**：用真實的測試數據對比，證明「純軟體最佳化」不需要升級顯示卡也能流暢運作。

## 8. Feature Grid — 四大避坑指引 (Trap Evading)
- **版型**：Feature Grid
- **重點**：
  - **模型選擇避坑**：8GB VRAM 下，`e4b` (MoE) 是唯一解。若選 `12B` dense 會導致 OOM 觸發 CPU offload。
  - **重複輸出避坑**：gemma4 易在長 context 下重複，必須在 Modelfile 設定 `repeat_penalty 1.15`。
  - **參數錯誤避坑**：LiteLLM 必須開啟 `drop_params: true` 過濾 Anthropic 專屬參數，防止 400 Bad Request。
  - **思考模式避坑**：必須將 Ollama 升級至 v0.9.0+，或在 LiteLLM 層阻斷 `reasoning_effort` 的轉發。
- **備註**：整理實戰踩坑經驗，讓聽眾回去部署時可以少走彎路。

## 9. Closing — 地端極限與下一步行動
- **版型**：Closing
- **重點**：
  - **已知限制**：對話長度有限，32K 剩餘空間少，適合「單檔案程式碼審查、局部 Bug 修正、語法解釋」。
  - **下一步行動**：
    - 專案已準備好 `start-optimized-env.sh` 一鍵啟動腳本。
    - 歡迎下載配置，開啟您的地端 Agent 實驗！
- **備註**：結尾收斂，說明地端並非萬能，但在特定場景下已具備極高的實用價值。
