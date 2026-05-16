# [Python] 用 fastembed + Qdrant 打造繁體中文職缺語意搜尋

工作搜尋平台的關鍵字搜尋，常常搜不到你真正想找的東西。你輸入「後端工程師 台北」，結果一堆不相關的職缺冒出來。語意搜尋 (Semantic Search) 就是用來解決這件事的——它搜的是「意思」，不是「字」。

這篇記錄我用 Python 爬取 1111 人力銀行的職缺資料，然後丟進 Qdrant 向量資料庫，最後用 fastembed 做語意搜尋的整個過程。

---

## 開發環境

- OS：Windows 11 + WSL2 Ubuntu
- Python：3.12
- 套件管理：uv 0.8.11
- fastembed：0.8.0
- qdrant-client：1.17.1
- Qdrant Server：Docker（localhost:6333）
- Embedding Model：`intfloat/multilingual-e5-large`（1024 維）

---

## 專案架構

本文聚焦的核心檔案大致如下：

```
Lab.Qdrant/
├── collect_1111_jobs.py        # 爬取 + enrichment，一步到位
├── import_1111_jobs_to_qdrant.py  # 產生 vector + 匯入 Qdrant
├── output/
│   └── 1111_jobs_page1_top100.enriched.json
└── pyproject.toml              # uv 管理依賴
```

---

## 資料餵入流程

先把整個資料餵入流程畫清楚，後面比較不會看一看就亂掉 XDD

```text
原始資料（文字 / 圖片）
 ↓
Embedding Model（OpenAI / HuggingFace / Ollama）
 ↓
向量 [0.1, 0.9, 0.2, ...]
 ↓
Qdrant upsert（向量 + payload）
 ↓
向量資料庫存放完成
 ↓
可做語意搜尋（Semantic Search）
```

這篇實際示範的是「職缺文字 → fastembed / HuggingFace 模型 → Qdrant」這條路，圖片 embedding 不是這篇的重點，就不多說了。

其中 `payload` 可以理解成這筆資料附帶的欄位資訊，像是職缺名稱、公司名稱、工作地點、待遇這些。之後除了做向量相似搜尋，也可以搭配 filter 一起查，不然資料一多很容易噴掉了啦!!!

---

## Step 1：建立 uv 環境

用 `uv init` 初始化專案。

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
- **enrich**：對每筆職缺爬取詳情頁 `https://www.1111.com.tw/job/{job_id}`，用 BeautifulSoup 先找到 `<h3>` 作為欄位標題，再往後解析對應內容，補齊人可讀欄位（薪資、學歷、駕照、電腦技能…）

兩者合併後，pipeline 變成：

```
API 取清單 → 逐筆爬詳情頁 → enriched JSON 輸出到 output/
```

最後輸出的 JSON 不是只有 `jobs` 陣列，外層還會帶 `count`、`source_url`、`notes`，方便後續匯入與追查資料來源。

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

首次執行會自動下載模型到本機快取：

```
[本機模型快取目錄]
└── intfloat/multilingual-e5-large
    ├── model.onnx
    └── tokenizer.json
```

實際快取路徑會依 `fastembed` 與執行環境而異；模型下載完成後，後續就能離線執行，不需要任何外部服務。

Embedding model 我這次改用 `intfloat/multilingual-e5-large`（1024 維），因為它在目前 `fastembed 0.8.0` 環境可正常使用，處理繁中與英文混合場景也沒有問題。實際匯入結果是 100 筆資料可正常寫入 Qdrant，`verify` 也能對到 100/100。

---

## Step 4：匯入 Qdrant（import_1111_jobs_to_qdrant.py）

這支腳本做四件事，用子命令切分：

```bash
uv run import_1111_jobs_to_qdrant.py import    # 匯入
uv run import_1111_jobs_to_qdrant.py verify    # 驗證筆數
uv run import_1111_jobs_to_qdrant.py search --query "會計 彰化 全職"  # 語意搜尋
uv run import_1111_jobs_to_qdrant.py prepare   # 預覽 prepare 後的 text 與 payload
```

### import 流程

先把每筆職缺的關鍵欄位組成一段文字，除了職缺名稱、公司名稱、待遇、工作內容，也把工作性質、學歷要求、工作經驗、技能等欄位一起串進去，讓 embedding 抓到更多語意線索：

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
model = TextEmbedding(model_name="intfloat/multilingual-e5-large")
vectors = list(model.embed([job.text for job in prepared_jobs]))

points = [
    PointStruct(id=job.id, vector=vector.tolist(), payload=job.payload)
    for job, vector in zip(prepared_jobs, vectors)
]
client.upsert(collection_name=collection_name, points=points, wait=True)
```

實際執行時，`fastembed` 會提示這個 model 在目前版本使用 **mean pooling**；這是套件提示訊息，不會阻擋匯入流程。

Collection 建立時也順手設好 payload index，像是 `job_id`、`company_name`、`industry`、`work_city`、`role_labels`、`job_type_labels`、`salary_min`、`salary_max`，讓後續 filter 有索引可用：

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

這裡先示範最基本的 dense search + filter，Hybrid Search 與 reranking 不是這篇的重點，就先不展開。

---

## 心得

- fastembed 封裝得很乾淨，不用自己處理 tokenizer 和 pooling，三行就能產生 vector
- uv 取代傳統 virtualenv 的組合，開發體驗好很多，`uv run` 直接跑不需要 activate
- 1111 的 API 沒有公開文件，參數要自己觀察 Network tab 去猜，有時候結果會重複，所以爬取時要做去重（用 `seen_ids`）
- `intfloat/multilingual-e5-large` 在目前環境可正常下載與執行，但模型體積比 small 大很多，首次下載時間會比較久，向量維度也變成 1024
- Qdrant 的 payload index 如果不設，filter 還是能用，只是沒有索引會全掃，資料量大時要注意

---

完整代碼位置: https://github.com/yaochangyu/sample.dotblog/tree/master/Qdrant/Lab.Qdrant

---

若有謬誤,煩請告知,新手發帖請多包涵
