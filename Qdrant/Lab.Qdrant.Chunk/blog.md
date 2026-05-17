# [Qdrant] HTML 文章切塊（Chunking）要怎麼做？

把 HTML 文章直接丟進 Embedding model，通常不會有好下場。原因很簡單：HTML 裡面混了導覽列、側邊欄、推薦文章、分享按鈕、廣告，這些東西如果一起做 embedding，搜尋結果很容易歪掉。這篇整理 HTML 文章在做向量化前，切塊（Chunking）應該怎麼處理，重點放在「先抽正文，再依結構切塊」，不要一開始就用固定字數硬切。

---

## 開發環境

- OS：Windows 11 + WSL2 Ubuntu
- Python：3.12
- 套件管理：uv 0.8.11
- beautifulsoup4：4.14.3
- qdrant-client：1.17.1
- 向量資料庫：Qdrant

---

## 核心觀念

先講結論，HTML 文章的切塊不要直接對原始 HTML 字串下刀，應該拆成兩段：

1. **抽出文章正文**
2. **依文件結構切成 chunk**

原因是 chunk 的邊界應該盡量落在**標題、段落、小節**，而不是落在任意 500 個字後面。切太小，上下文不夠；切太大，主題被稀釋，兩邊都容易讓檢索品質掉下來。

一個實務上很好用的原則是：**每個 chunk 只表達一個主題，而且查詢時可以被單獨命中。**

---

## Step 1：先抽出正文，不要把整頁 HTML 直接拿去切

HTML 頁面通常會長這樣：

```html
<header>...</header>
<nav>...</nav>
<main>
  <article>
    <h1>Qdrant Chunking 筆記</h1>
    <p>Chunking 不是固定切字數...</p>
  </article>
</main>
<aside>推薦文章</aside>
<footer>...</footer>
```

如果你把整頁都拿去 embedding，`nav`、`aside`、`footer` 這些雜訊也會一起進去，結果就很容易噴掉了啦!!!

這段程式碼是先抓出正文節點，只保留 `article`、`main` 這類可能的主體區塊。

```python
from bs4 import BeautifulSoup


def 取得文章主體(html: str):
    soup = BeautifulSoup(html, "html.parser")

    for selector in ["script", "style", "nav", "footer", "aside", "form"]:
        for node in soup.select(selector):
            node.decompose()

    article = soup.find("article")
    if article is not None:
        return article

    main = soup.find("main")
    if main is not None:
        return main

    return soup.body or soup
```

如果網站結構很固定，建議直接指定 CSS selector 抽正文，會比全頁猜測更穩。這不是這一篇的重點，就不多說了。

---

## Step 2：把 HTML 轉成結構化 block

正文抓出來之後，下一步不是立刻切字數，而是先轉成 block。常見 block 可以這樣分：

- `h1` ~ `h6`：標題
- `p`：段落
- `li`：清單項目
- `pre`、`code`：程式碼區塊
- `table`：表格

這段程式碼是把正文節點轉成可後續切塊的 block 清單。

```python
from dataclasses import dataclass


@dataclass
class Block:
    kind: str
    text: str
    section: str


def 解析區塊(root) -> list[Block]:
    blocks: list[Block] = []
    current_section = "未分類"

    for node in root.find_all(["h1", "h2", "h3", "h4", "h5", "h6", "p", "li", "pre", "table"]):
        text = node.get_text(" ", strip=True)
        if not text:
            continue

        if node.name in {"h1", "h2", "h3", "h4", "h5", "h6"}:
            current_section = text
            continue

        kind = "code" if node.name == "pre" else node.name
        blocks.append(Block(kind=kind, text=text, section=current_section))

    return blocks
```

這裡有兩個重點：

1. **標題本身是切塊邊界資訊**
2. **程式碼區塊、表格通常要視為不可拆單元**

不然你把 `pre` 從中間切開，後面查到的 chunk 很可能只剩半段 code，看了會很痛苦 XDD

---

## Step 3：依主題組 chunk，不要只看固定字數

很多人第一版都會這樣做：

1. 把全文轉純文字
2. 每 500 字切一塊
3. 每塊 overlap 50 字

這招不是不能用，但對 HTML 文章通常太粗。比較合理的方式是：

1. 先按 section 聚合
2. 再把相鄰段落合併
3. 接近大小門檻時才切新 chunk

這段程式碼是把 block 組成 chunk，並保證每塊盡量只談一個主題。

```python
from dataclasses import dataclass


@dataclass
class Chunk:
    chunk_id: int
    section: str
    text: str


def 建立切塊(blocks: list[Block], max_chars: int = 800) -> list[Chunk]:
    chunks: list[Chunk] = []
    buffer: list[str] = []
    current_section = "未分類"
    chunk_id = 1

    for block in blocks:
        block_text = block.text.strip()
        if not block_text:
            continue

        section_changed = buffer and block.section != current_section
        next_length = len("\n".join(buffer + [block_text]))

        if section_changed or next_length > max_chars:
            chunks.append(
                Chunk(
                    chunk_id=chunk_id,
                    section=current_section,
                    text="\n".join(buffer).strip(),
                )
            )
            chunk_id += 1
            buffer = []

        current_section = block.section
        buffer.append(block_text)

    if buffer:
        chunks.append(
            Chunk(
                chunk_id=chunk_id,
                section=current_section,
                text="\n".join(buffer).strip(),
            )
        )

    return chunks
```

`max_chars` 只是保護機制，不是唯一切點。真正的切點，應該優先由 **section 邊界** 與 **主題完整性** 來決定。

---

## Step 4：補 overlap，但不要補到太重

overlap 的目的不是複製一大堆內容，而是避免關鍵句剛好落在 chunk 邊界。

這段程式碼是幫新 chunk 補上前一塊最後一小段文字。

```python
def 加上重疊(chunks: list[Chunk], overlap_chars: int = 120) -> list[Chunk]:
    result: list[Chunk] = []

    for index, chunk in enumerate(chunks):
        if index == 0:
            result.append(chunk)
            continue

        previous = chunks[index - 1].text
        prefix = previous[-overlap_chars:].strip()
        merged_text = f"{prefix}\n{chunk.text}".strip()

        result.append(
            Chunk(
                chunk_id=chunk.chunk_id,
                section=chunk.section,
                text=merged_text,
            )
        )

    return result
```

如果 overlap 太大，副作用也很明顯：

- 儲存成本變高
- 重複內容變多
- 搜尋結果可能出現太多很像的 chunk

所以 overlap 要有，但通常只要補一小段上下文就夠了。

---

## Step 5：寫入 Qdrant 時，把 metadata 一起帶進去

chunk 不只是文字，還要保留能追來源與做 filter 的 metadata。

這段程式碼是把 chunk 轉成可寫入向量資料庫的 payload。

```python
def 建立Payload(doc_id: str, source: str, chunks: list[Chunk]) -> list[dict]:
    payloads: list[dict] = []

    for chunk in chunks:
        payloads.append(
            {
                "doc_id": doc_id,
                "chunk_id": chunk.chunk_id,
                "source": source,
                "section": chunk.section,
                "language": "zh-TW",
                "text": chunk.text,
            }
        )

    return payloads
```

最少建議保留這幾個欄位：

- `doc_id`
- `chunk_id`
- `source`
- `section`
- `language`

如果一開始 metadata 沒設計好，後面要補來源追蹤、權限控制、分類過濾，通常都會補到很痛苦。

---

## 心得

- HTML 文章的 chunking，本質上不是「切字串」，而是「切文件結構」
- `article` / `main` 正文抽取，通常比固定字數切塊更重要
- `pre`、`table` 這類 block 最好不要拆開
- overlap 要有，但不用太大，不然重複內容會很多
- 如果文章有明確標題層級，直接拿 `h1` ~ `h6` 當 section 邊界，效果通常就不錯

---

完整代碼位置: https://github.com/yaochangyu/sample.dotblog/tree/master/Qdrant/Lab.Qdrant.Chunk

---

若有謬誤,煩請告知,新手發帖請多包涵
