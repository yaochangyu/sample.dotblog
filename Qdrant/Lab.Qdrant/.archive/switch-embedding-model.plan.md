# Switch Embedding Model Plan

- [x] 更新 `import_1111_jobs_to_qdrant.py` 的預設 embedding model 為 `intfloat/multilingual-e5-large`，因為目前實際執行 `fastembed 0.8.0` 時，`intfloat/multilingual-e5-small` 不受支援，主流程會在匯入與搜尋階段失敗。
- [x] 更新 `import_1111_jobs_to_qdrant.ipynb` 的 model 設定，因為 notebook 目前也寫死 `intfloat/multilingual-e5-small`，若不一起調整，CLI 與 notebook 的行為會不一致。
- [x] 更新 `blog.md` 內文與環境說明，因為文件目前仍寫 `intfloat/multilingual-e5-small` 與 384 維，若不修正，文件將與專案實際可執行狀態不符。
- [x] 檢查並調整測試或驗證流程需要引用的 model 名稱，因為整合查詢流程目前會跟著預設 model 走，必須確認改動後驗證方式仍可重現。
- [x] 重新執行既有 build / 驗證指令並記錄結果，因為這次變更會影響匯入結果的向量維度與搜尋前置設定，需要再次確認 CLI 匯入流程能正常完成。
