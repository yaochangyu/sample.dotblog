# 實作計畫：GitCommitPush - 提交與推送異動

本計畫旨在提交並推送最近對 `blog.md`、`README.md`、`@tree.md` 以及歸檔計畫檔案的異動，並遵循專案的 git commit 規範。

## 實作步驟

- [x] **步驟 1：確認是否需要 Ticket ID** <!-- id: 1 -->
  - **說明**：詢問使用者是否需要為此次 commit 加上 ticket id。

- [x] **步驟 2：撰寫並確認 Git Commit Message** <!-- id: 2 -->
  - **說明**：依據 staged diff 內容，依照 `[EMOJI] [TYPE](file/topic)(ticket id)): [description]` 格式撰寫 commit message，並提供給使用者審查。

- [x] **步驟 3：執行 Git Commit & Git Push** <!-- id: 3 -->
  - **說明**：執行 `git commit` 與 `git push`，將異動推送至遠端倉庫。

- [x] **步驟 4：更新專案結構檔 `@tree.md`** <!-- id: 4 -->
  - **說明**：將 `GitCommitPush.plan.md` 加入結構中。

- [x] **步驟 5：封存計畫檔案** <!-- id: 5 -->
  - **說明**：完成所有步驟後，將 `GitCommitPush.plan.md` 移動至 `.archive` 資料夾，並更新 `@tree.md`。
