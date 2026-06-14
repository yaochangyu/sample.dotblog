# 實作計畫：MoveCaptureProxy - 移動 capture_proxy.py 至 script 目錄

本計畫旨在將診斷工具 `capture_proxy.py` 收納至 `script/` 資料夾下，並更新相關文件與專案結構。

## 實作步驟

- [x] **步驟 1：移動 `capture_proxy.py` 到 `script/` 目錄** <!-- id: 1 -->
  - **說明**：執行 `mv` 指令，將根目錄下的 `capture_proxy.py` 移動到 `script/capture_proxy.py`，保持根目錄乾淨並統一腳本存放位置。

- [x] **步驟 2：更新 `README.md` 中的檔案說明路徑** <!-- id: 2 -->
  - **說明**：將 `README.md` 中的 `capture_proxy.py` 改為 `script/capture_proxy.py`。

- [x] **步驟 3：更新專案結構檔 `@tree.md`** <!-- id: 3 -->
  - **說明**：在 `@tree.md` 中將 `capture_proxy.py` 移動至 `script/` 目錄下，並加入 `MoveCaptureProxy.plan.md`。

- [x] **步驟 4：更新 Git 暫存區狀態** <!-- id: 4 -->
  - **說明**：將移動後的 `script/capture_proxy.py`、修改後的 `README.md` 和 `@tree.md` 加入 staged，並確認 git status 正確。

- [x] **步驟 5：封存計畫檔案** <!-- id: 5 -->
  - **說明**：完成所有步驟後，將 `MoveCaptureProxy.plan.md` 移動至 `.archive` 資料夾，並更新 `@tree.md`。
