# blog.md 更新計畫

- [x] **步驟 1：比對文章與目前程式碼差異**
  - 為何需要：先確認 `blog.md` 哪些段落已經和目前實作不一致，避免直接修改時漏掉 `TtlJitterCacheService`、`CacheWarmupService`、整合測試與套件版本等內容。

- [x] **步驟 2：更新 `blog.md` 內容**
  - 為何需要：把文章中的程式碼片段、說明文字、套件安裝與測試描述調整成和現況一致，特別是 TTL Jitter 目前是在 `GetAsync()` 內動態建立 `HybridCacheEntryOptions`。

- [x] **步驟 3：檢查文件一致性並收尾**
  - 為何需要：確認修改後的文章前後敘述一致，並在完成後封存計畫檔、同步 `tree.md`，避免文件狀態與專案現況脫節。
