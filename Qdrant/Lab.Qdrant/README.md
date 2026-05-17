# Lab.Qdrant

用 Python 載入 Kaggle 上的 104 人力銀行職缺資料集，匯入 Qdrant 向量資料庫，實現繁體中文語意搜尋。

## 架構

```
collect_1111_jobs.py          import_104_jobs_to_qdrant.py
──────────────────────        ──────────────────────────────
1111 API（另一條資料來源）       Kaggle 104 資料集
    ↓                              ↓
爬職缺詳情頁（BeautifulSoup）  fastembed 產生 vector（ONNX Runtime）
    ↓                              ↓
output/*.enriched.json    →   Qdrant upsert / search / verify
```

## 開發環境

- Python 3.12
- uv 0.8.11
- fastembed 0.8.0
- qdrant-client 1.17.1
- Qdrant Server：Docker（localhost:6333）
- Embedding Model：`intfloat/multilingual-e5-large`（1024 維）

## 快速開始

### 1. 建立環境

```bash
uv sync
```

### 2. 啟動 Qdrant

```bash
docker compose up -d
```

### 3. （可選）爬取 1111 職缺

```bash
# 如果你還要保留 1111 那條流程，可以另外執行這支腳本
uv run collect_1111_jobs.py

# 自訂參數
uv run collect_1111_jobs.py --start-page 3 --count 50
```

### 4. 匯入 Qdrant

```bash
uv run import_104_jobs_to_qdrant.py import
uv run import_104_jobs_to_qdrant.py import --recreate   # 重建 collection
```

### 5. 驗證

```bash
uv run import_104_jobs_to_qdrant.py verify
```

### 6. 語意搜尋

```bash
uv run import_104_jobs_to_qdrant.py search --query "會計 全職 台北"
uv run import_104_jobs_to_qdrant.py search --query "軟體工程師" --city "台北市" --limit 10
```

## 腳本說明

| 腳本 | 說明 |
|------|------|
| `collect_1111_jobs.py` | 呼叫 1111 API 取清單，再爬每筆詳情頁補齊欄位，輸出 enriched JSON |
| `import_104_jobs_to_qdrant.py` | 下載 Kaggle 104 資料集（或讀自訂輸入）→ 產生 vector → 匯入 Qdrant，支援 import / verify / search / prepare |
| `qdrant_104_jobs_workbench.ipynb` | Jupyter 工作台，直接引用 `import_104_jobs_to_qdrant.py` 的函式 |
| `test_import_104_jobs_to_qdrant.py` | 單元測試 + 整合測試 |

## Embedding Model

使用 `intfloat/multilingual-e5-large`，專為**檢索任務**設計的多語言模型，繁體中文覆蓋良好，1024 維，且已在目前專案環境成功完成匯入與查詢流程。

fastembed 底層使用 ONNX Runtime，首次執行自動下載模型到 `~/.cache/huggingface/`，之後離線執行，不需要 PyTorch 或外部 API。

## 完整代碼

https://github.com/yaochangyu/sample.dotblog/tree/master/Qdrant/Lab.Qdrant
