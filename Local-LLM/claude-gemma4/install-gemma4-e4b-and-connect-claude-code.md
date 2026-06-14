# 在本機安裝 Gemma 4 E4B，讓 Claude Code 串接 Ollama

最近 Google 釋出了 Gemma 4 系列模型，其中 E4B 是針對邊緣裝置最佳化的版本，4.5B 有效參數，跑在筆電上沒什麼壓力。

這篇記錄一下怎麼用 Ollama 在本機跑 `gemma4:e4b`，並且讓 Claude Code 直接串接這個本地端模型，省下一些 API token XDD

---

## 開發環境

- OS：Windows 11 / Ubuntu（WSL2）
- Ollama：latest
- Claude Code：latest
- Model：`gemma4:e4b`

---

## Gemma 4 E4B 模型介紹

先簡單介紹一下這顆模型，包含：

- **有效參數**：4.5B（含 embedding 共約 8B）
- **模型大小**：9.6 GB（Q4_K_M 量化）
- **Context 長度**：128K tokens
- **授權**：Apache License 2.0
- **支援輸入**：文字、圖片、音訊

NOTE：E 系列（E2B、E4B）是 Mixture of Experts 架構，E 代表有效（Effective）參數，專為邊緣設備設計，所以在一般筆電上也能跑得動。

---

## 安裝 Ollama

前往 [https://ollama.com/download](https://ollama.com/download) 下載對應你作業系統的安裝包，支援 macOS、Windows、Linux。

安裝完成後，確認 Ollama 有在運行：

```bash
ollama --version
```

---

## 安裝 gemma4:e4b

下面這個指令會直接拉取並執行模型，第一次執行會先下載（約 9.6 GB），請確保磁碟空間足夠：

```bash
ollama run gemma4:e4b
```

拉完之後就可以在終端機直接跟模型對話，確認模型正常運作後按 `Ctrl+D` 離開。

---

## 讓 Claude Code 串接 Ollama

Ollama 提供兩種方式讓 Claude Code 指向本地端模型，我分別說明。

### 方法一：ollama launch claude（快速啟動）

這是官方提供的最簡單方式，一行指令搞定：

```bash
ollama launch claude
```

這個指令會自動幫你設好環境變數，然後啟動 Claude Code 並指向本地 Ollama 服務。

### 方法二：手動設定環境變數

如果你希望自己控制設定，或者 `ollama launch claude` 不符合你的工作流程，可以手動設：

```bash
export ANTHROPIC_AUTH_TOKEN=ollama
export ANTHROPIC_API_KEY=""
export ANTHROPIC_BASE_URL=http://localhost:11434
```

設好之後，啟動 Claude Code 並指定模型：

```bash
claude --model gemma4:e4b
```

NOTE：`ANTHROPIC_BASE_URL` 要指向本機 Ollama 的 API 端點，預設是 `http://localhost:11434`。

---

## 注意事項

- Claude Code 需要至少 **64K tokens** 的 context window，`gemma4:e4b` 支援 **128K**，完全沒問題。
- 模型大小 9.6 GB，RAM 建議至少 12GB 以上，否則會頻繁 swap，速度會很慢。
- 本地模型的回應速度取決於你的硬體，不要拿來跟 Claude API 比速度 XDD

---

## 參考資料

- [gemma4:e4b — Ollama Library](https://ollama.com/library/gemma4:e4b)
- [Claude Code Integration — Ollama Docs](https://docs.ollama.com/integrations/claude-code)
- [Gemma 4 — Google DeepMind](https://deepmind.google/models/gemma/gemma-4/)

---

若有謬誤,煩請告知,新手發帖請多包涵
