# [Python] 用 fastembed + Qdrant 打造繁體中文職缺語意搜尋

工作搜尋平台的關鍵字搜尋，常常搜不到你真正想找的東西。你輸入「後端工程師台北」，結果一堆不相關的職缺冒出來。語意搜尋 (Semantic Search) 就是用來解決這件事的——它搜的是「意思」，不是「字」。

這篇記錄我用 Python 爬取 1111 人力銀行的職缺資料，然後丟進 Qdrant 向量資料庫，最後用 fastembed 做語意搜尋的整個過程。

---

## 開發環境

- OS：Windows 11 + WSL2 Ubuntu
- Python：3.12
- 套件管理：uv 0.8.11
- fastembed：0.8.0
- qdrant-client：1.17.1
- Qdrant Server：Docker（localhost:6333）
- Embedding Model：`intfloat/multilingual-e5-small`（384 維）

---

## 專案架構

最終的資料夾結構長這樣：

```
Lab.Qdrant/
├── collect_1111_jobs.py        # 爬取 + enrichment，一步到位
├── import_1111_jobs_to_qdrant.py  # 產生 vector + 匯入 Qdrant
├── output/
│   └── 1111_jobs_page1_top100.enriched.json
└── pyproject.toml              # uv 管理依賴
```

---

## Step 1：建立 uv 環境

用 `uv init` 初始化專案，依賴統一寫進 `pyproject.toml`，鎖定版本則交給 `uv.lock` 管理。

```bash
uv init --no-readme --python 3.12
uv add beautifulsoup4 fastembed qdrant-client requests
```

之後執行腳本一律用 `uv run`，不需要手動 activate 虛擬環境：

```bash
uv run collect_1111_jobs.py
```

---

## Step 2：爬取職缺資料（collect_1111_jobs.py）

- **fetch**：呼叫 1111 的非公開 API `https://www.1111.com.tw/api/v1/search/jobs/`，回傳 JSON，批次取得職缺清單
- **enrich**：對每筆職缺爬取詳情頁 `https://www.1111.com.tw/job/{job_id}`，用 BeautifulSoup 解析 `<h3>` 標籤補齊人可讀欄位（薪資、學歷、駕照、電腦技能…）

兩者合併後，pipeline 變成：

```
API 取清單 → 逐筆爬詳情頁 → enriched JSON 輸出到 output/
```

執行方式：

```bash
# 預設：從第 1 頁取 100 筆
uv run collect_1111_jobs.py

# 自訂參數
uv run collect_1111_jobs.py --start-page 3 --count 50
```

輸出路徑動態產生，`--start-page 1 --count 100` 就輸出 `output/1111_jobs_page1_top100.enriched.json`。

---

## Step 3：認識 fastembed

fastembed 是 Qdrant 官方出的 Python 套件，底層就是 **ONNX Runtime**，不需要裝 PyTorch。

```
fastembed
├── onnxruntime      ← 推理引擎
├── tokenizers       ← HuggingFace tokenizer
└── huggingface-hub  ← 首次執行自動下載模型
```

首次執行會從 HuggingFace 下載模型快取到本機：

```
~/.cache/huggingface/hub/
└── models--intfloat--multilingual-e5-small/
    ├── onnx/model.onnx
    └── tokenizer.json
```

之後就離線執行，不需要任何外部服務。

Embedding model 選 `intfloat/multilingual-e5-small`（384 維），它是專為**檢索任務**設計的多語言模型，繁體中文覆蓋比純中文的 `BAAI/bge-small-zh-v1.5` 好。

---

## Step 4：匯入 Qdrant（import_1111_jobs_to_qdrant.py）

這支腳本做四件事，用子命令切分：

```bash
uv run import_1111_jobs_to_qdrant.py import    # 匯入
uv run import_1111_jobs_to_qdrant.py verify    # 驗證筆數
uv run import_1111_jobs_to_qdrant.py search --query "會計 彰化 全職"  # 語意搜尋
uv run import_1111_jobs_to_qdrant.py prepare   # 預覽 embedding text
```

### import 流程

先把每筆職缺的關鍵欄位組成一段文字，讓 embedding 抓得到語意：

```python
sections = [
    ("職缺名稱", job.get("title")),
    ("公司名稱", job.get("companyName")),
    ("產業",     job.get("industry", {}).get("name")),
    ("工作地點", job.get("workCity", {}).get("name")),
    ("職務類別", join_values(enum_meaning.get("roleLabels"))),
    ("待遇",     job.get("salary")),
    ("工作內容", job.get("description")),
    # ... 其他欄位
]
```

然後用 fastembed 批次產生 vector，寫入 Qdrant：

```python
model = TextEmbedding(model_name="intfloat/multilingual-e5-small")
vectors = list(model.embed([job.text for job in prepared_jobs]))

points = [
    PointStruct(id=job.id, vector=vector.tolist(), payload=job.payload)
    for job, vector in zip(prepared_jobs, vectors)
]
client.upsert(collection_name=collection_name, points=points, wait=True)
```

Collection 建立時也順手設好 payload index，讓後續 filter 有索引可用：

```python
for field_name, schema in [
    ("work_city",  PayloadSchemaType.KEYWORD),
    ("salary_min", PayloadSchemaType.INTEGER),
    ("salary_max", PayloadSchemaType.INTEGER),
    # ...
]:
    client.create_payload_index(...)
```

### search 流程

query 文字也要先過一次 embedding，再做向量相似搜尋：

```python
query_vector = list(model.query_embed("會計 全職 彰化"))[0].tolist()

results = client.query_points(
    collection_name=collection_name,
    query=query_vector,
    query_filter=Filter(must=[
        FieldCondition(key="work_city", match=MatchValue(value="彰化縣二林鎮"))
    ]),
    limit=5,
)
```

---

## 心得

- fastembed 封裝得很乾淨，不用自己處理 tokenizer 和 pooling，三行就能產生 vector
- uv 取代傳統 virtualenv 的組合，開發體驗好很多，`uv run` 直接跑不需要 activate
- 1111 的 API 沒有公開文件，參數要自己觀察 Network tab 去猜，有時候結果會重複，所以爬取時要做去重（用 `seen_ids`）
- `intfloat/multilingual-e5-small` 第一次下載約 120MB，之後快取在本機，速度還可以接受
- Qdrant 的 payload index 如果不設，filter 還是能用，只是沒有索引會全掃，資料量大時要注意

---

完整代碼位置: https://github.com/yaochangyu/sample.dotblog/tree/master/Qdrant/Lab.Qdrant

---

若有謬誤,煩請告知,新手發帖請多包涵
