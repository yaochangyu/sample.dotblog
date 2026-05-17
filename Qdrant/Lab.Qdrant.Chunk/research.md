# blog.md 研究結果

## 已知事實

1. 目前最有效的例子主要集中在 **Step 1、Step 3、Step 4**。
2. **Step 1（L31-L72）** 展示了先抽正文、排除 `nav`、`aside`、`footer` 等雜訊的做法，適合作為 HTML chunking 的起手式。
3. **Step 3（L129-L194）** 用 `section_changed` 與 `max_chars` 控制切塊邊界，最貼近文章主軸。
4. **Step 4（L200-L227）** 示範 overlap 的基本做法，並且有把 overlap 過大的副作用寫出來。
5. 先前學生 agent 懷疑 **Step 3 的 `建立切塊()` 有 section metadata bug**，補查後確認：**邏輯正確，不是 bug**，只是可讀性比較差。
6. **Step 2（L78-L126）** 有 block 解析概念，但缺少更具體的輸出例子，對巢狀結構的說明也不夠。
7. **Step 5（L240-L263）** 只有 `payload` 組裝，缺少 embedding、Qdrant 寫入與查詢驗證的完整流程。
8. 目前這個資料夾本機只有 `blog.md`、`tree.md`，沒有可執行的 Python 範例檔可支持 Step 5。

## 推論

1. 這篇文章目前最有力的主線是 **先抽正文，再依結構切塊**，重點沒有抓歪。
2. Step 1、Step 3、Step 4 已經可以當主例子，但 Step 2、Step 5 還不足以讓讀者直接照著完整實作。
3. Step 1 的 `return soup.body or soup` 比較像合理 fallback，不是明顯錯誤；但它偏寬鬆，可能把非正文內容一起帶進來。

## 建議

1. 保留 **Step 1、Step 3、Step 4** 當主要研究例子。
2. 優先補強 **Step 2**，加入 block 解析後的實際輸出例子。
3. 優先補強 **Step 5**，補上 `chunk -> embedding -> payload -> Qdrant upsert` 的完整流程。
4. 不需要再追 Step 3 的 section metadata 問題，這塊已經補查完成。
