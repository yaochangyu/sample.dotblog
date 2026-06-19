# 移除 Commit Message 中的 Co-authored-by 計畫

此計畫旨在備份當前分支，改寫 Git 歷史紀錄以移除 `Co-authored-by` 欄位，並將修改後的分支推送至遠端。

## 計畫步驟

- [x] **步驟 1：建立並推送備份分支**
  - **說明**：在對 Git 歷史進行 any 改寫前，先建立一個本地備份分支 `backup/master` 並推送到遠端倉庫 `origin`，以防止改寫失敗時無法還原。
- [x] **步驟 2：改寫歷史移除 Co-authored-by**
  - **說明**：使用 `git filter-branch` 在 `master` 分支上批量過濾並刪除 commit message 中的 `Co-authored-by` 行。
- [x] **步驟 3：驗證改寫結果**
  - **說明**：使用 `git log` 檢查是否所有 `Co-authored-by` 均已成功移除，且確認專案的 commit 歷史與檔案結構是否正常。
- [x] **步驟 4：強制推送改寫後的分支到遠端**
  - **說明**：將修改完成的本地 `master` 分支強制推送至遠端 `origin/master`，使遠端歷史與本地同步。
