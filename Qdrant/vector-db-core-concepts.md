# 向量資料庫核心關鍵與現存問題

## 資料餵入流程

```
原始資料 (文字/圖片)
    ↓
Embedding Model (OpenAI / HuggingFace / Ollama)
    ↓
向量 [0.1, 0.9, 0.2, ...]
    ↓
Qdrant upsert (向量 + payload)
    ↓
向量資料庫存放完成 → 可做語意搜尋
```

---

## 五個核心關鍵

### 1. Embedding（向量化）

資料進入向量空間的入口。所有非結構化資料必須先轉成向量才能存入。
Embedding Model 的品質直接決定語意理解的上限，選錯模型，後面全錯。

**存入與查詢必須使用同一個模型。**

語意相近的句子，向量距離近；語意不相關的句子，向量距離遠：

```
"貓喜歡曬太陽"  →  [0.12, 0.87, 0.03, ...]
"狗喜歡奔跑"    →  [0.11, 0.79, 0.04, ...]  ← 距離近（動物日常）
"股票大漲"      →  [0.91, 0.02, 0.55, ...]  ← 距離遠（完全不同語意）
```

常見模型比較：

| 模型 | 維度 | 特色 |
|------|------|------|
| `text-embedding-3-small` (OpenAI) | 1536 | 精準，需付費 |
| `BAAI/bge-m3` | 1024 | 多語系，支援中文，免費本地跑 |
| `intfloat/multilingual-e5-small` | 384 | 輕量多語系，適合繁體中文 |
| `nomic-embed-text` (Ollama) | 768 | 本地跑，隱私保護 |

---

### 2. ANN Index — HNSW

搜尋效能的核心。暴力搜尋在百萬筆資料時完全不可行。

HNSW（Hierarchical Navigable Small World）用圖結構分層導航，以「近似最近鄰」換取毫秒級回應。

```
精準度 100%（Full Scan）  ←→  速度極快（HNSW 近似搜尋）
```

HNSW 的代價是需要大量 RAM 儲存圖結構，且新增資料時需重新計算鄰居關係，資料量越大索引建立越耗時。

---

### 3. Distance Metric（距離度量）

定義「像不像」的標準。**存入時選定，之後不能改。**

| 距離函數 | 適用場景 |
|----------|----------|
| Cosine | 文字語意，最常用 |
| Dot Product | 推薦系統、已正規化向量 |
| Euclidean | 圖像、空間資料 |

---

### 4. Hybrid Search（混合搜尋）

純語意搜尋對專有名詞（型號、人名、地名）容易搜不精準，Hybrid Search 結合語意與關鍵字兩路搜尋，讓 Recall 更高。

#### 為什麼需要混合搜尋？

| 方法 | 優點 | 缺點 |
|------|------|------|
| **Dense（語意）** | 理解同義詞、上下文 | 專有名詞、型號容易搜不到 |
| **Sparse（關鍵字）** | 精準比對特定詞彙 | 不理解語意，同義詞找不到 |
| **Hybrid** | 兩者互補 | 設定稍複雜 |

#### 執行流程（Prefetch 兩路）

Hybrid Search 的關鍵是「先各自搜尋、再融合排名」，Qdrant 用 `prefetch` 實現兩路並行：

```
查詢文字
    ├── 路一：Sparse（BM25/BM42）關鍵字 → 各自找 20 筆候選
    └── 路二：Dense 語意向量           → 各自找 20 筆候選
                    ↓
             RRF 融合排名（從 40 筆候選重新排）
                    ↓
              最終 Top 10
```

#### Sparse 方法：BM25 vs BM42

| 方法 | 說明 |
|------|------|
| **BM25** | 經典關鍵字演算法，純詞頻統計 |
| **BM42** | Qdrant 改良版，加入 Attention 權重，能判斷哪個詞更重要，準確度更高 |

#### 融合演算法：RRF（Reciprocal Rank Fusion）

RRF 只看排名，不管兩路分數的單位差異，直接用公式融合：

```
RRF 分數 = Σ 1 / (排名 + 60)
```

兩路都出現的結果會被加分，代表語意相關且關鍵字也吻合，是最可信的結果。K=60 是公認預設值，用來平滑排名避免第一名獨大。

#### 基礎 Hybrid 實作範例

```python
from fastembed import SparseTextEmbedding, TextEmbedding
from qdrant_client import QdrantClient, models

dense_model  = TextEmbedding(model_name="jinaai/jina-embeddings-v2-base-en")
sparse_model = SparseTextEmbedding(model_name="Qdrant/bm42-all-minilm-l6-v2-attentions")

query_text = "iPhone 電池好不好用"
dense_vec  = list(dense_model.query_embed(query_text))[0].tolist()
sparse_vec = list(sparse_model.query_embed(query_text))[0]

results = client.query_points(
    collection_name="my-collection",
    prefetch=[
        models.Prefetch(query=sparse_vec.as_object(), using="sparse", limit=20),
        models.Prefetch(query=dense_vec,              using="dense",  limit=20),
    ],
    query=models.FusionQuery(fusion=models.Fusion.RRF),
    limit=10
)
```

#### 三個層次比較

Hybrid Search 不只有一種做法，依精準度需求可選擇不同層次：

| 層次 | 方法 | 精準度 | 速度 | 適用場景 |
|------|------|--------|------|----------|
| 1 | 純 Dense | 中 | 最快 | 資料量小、快速驗證 |
| 2 | Dense + Sparse + RRF | 高 | 中 | 大多數生產環境 |
| 3 | Dense + Sparse + ColBERT Rerank | 最高 | 較慢 | 高精準需求、醫療/法律 |

#### 層次三：ColBERT Late Interaction Reranking

ColBERT 是 Late Interaction 模型，不像一般 Embedding 把整句壓成一個向量，它保留**每個 token 的向量**，查詢時做 token 級別比對，精準度最高但儲存成本也最高。

```
一般 Dense：  "後端工程師" → [單一向量]

ColBERT：     "後" → [向量1]
              "端" → [向量2]
              "工程師" → [向量3]
              → 每個 token 對每個 token 比對
```

```python
# Dense + Sparse 先撈候選，ColBERT 最終精排
prefetch = [
    models.Prefetch(query=dense_vec,  using="dense",  limit=20),
    models.Prefetch(query=sparse_vec, using="bm25",   limit=20),
]
results = client.query_points(
    collection_name="my-collection",
    prefetch=prefetch,
    query=colbert_query,       # ColBERT 對 40 筆候選重新精排
    using="colbertv2.0",
    limit=10,
)
```

---

### 5. Quantization（量化壓縮）

大規模部署的成本關鍵。

1536 維 float32 向量每筆佔 6KB，百萬筆 = 6GB RAM。
量化將向量壓縮（int8 / 1-bit），記憶體降低 4~32 倍，速度提升，精準度略降。

---

## 現存問題

### 問題一：近似 ≠ 精準（Recall 天花板）
HNSW 是「近似」搜尋，不保證找到真正最近的向量。
即使 Google Gemini 1.5 大 context window，Recall 仍低於 0.8，最多 **40% 的資訊可能遺失**。

### 問題二：記憶體成本高
高維向量必須放在 RAM 或 SSD 才能低延遲搜尋，資料量越大成本越高。ColBERT 因為儲存 token 級別向量，成本又更高一個數量級。

### 問題三：Embedding 一致性風險
模型升版、換供應商，舊資料必須**全部重新 Embedding**，成本極高。

### 問題四：Filtering + 向量搜尋的衝突
加上 metadata filter 時，HNSW 效能大幅下降，因為過濾縮小可搜尋範圍，ANN 優勢消失。

### 問題五：RAG 的幻覺問題未解
向量搜尋只負責「找到相關資料」，LLM 仍可能對找到的內容產生幻覺（Hallucination）。
**檢索品質好不代表生成結果正確，兩個環節需獨立評估。**

---

## 關鍵指標說明

| 指標 | 問的問題 |
|------|----------|
| **Recall** | 相關的有沒有都找出來？（應該找到的，實際找到了多少） |
| **Precision** | 找出來的有沒有都是相關的？（找出來的結果，有多少是真正相關的） |

兩者通常是 trade-off：找越廣，準確率越低；找越準，可能漏掉重要資料。

> RAG 系統建議：優先保 Recall（寧可多找，不要漏），再用 Reranking 提升 Precision。
