# Persistent Search Plan

- [x] 盤點現有搜尋流程與可重用函式，確認哪些初始化成本是每次 CLI 查詢都重跑。因為要把 20 秒級的查詢壓低到接近 1 秒，先找出真正的瓶頸，才不會改錯地方。
- [x] 在 `import_104_jobs_to_qdrant.py` 加入常駐互動查詢模式，讓 `TextEmbedding` 與 `QdrantClient` 只初始化一次。因為這是達成 1 秒內查詢的核心做法，之後每次只需要處理 query embedding 與 Qdrant search。
- [x] 補上對應測試或 smoke test，驗證常駐模式可以連續查詢且輸出正確結果。因為這次改的是執行模式，不只要能跑，還要確認連續輸入時不會壞掉。
- [x] 更新 `README.md`、`qdrant_104_jobs_workbench.ipynb` 或相關文件，補上常駐查詢模式的使用方式。因為功能改完後，如果文件沒跟上，之後還是很難正確使用。
