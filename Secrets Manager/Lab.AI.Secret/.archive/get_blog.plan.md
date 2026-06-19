# 從點部落取 blog.md 新版計畫

此計畫旨在透過 `metablog-cli` 工具，從點部落下載 postId 為 `abbb537b-b0fb-45e4-bc93-90730eec10dc` 的最新版文章，並用來覆蓋更新本地的 `blog.md` 檔案。

## 計畫步驟

- [x] **步驟 1：執行 metablog-cli 下載點部落最新文章**
  - **說明**：使用 `uv run metablog_cli.py get --ids abbb537b-b0fb-45e4-bc93-90730eec10dc` 下載最新文章到 output 目錄。
- [x] **步驟 2：更新本地的 blog.md**
  - **說明**：將下載的 Markdown 檔案覆蓋更新至 `/mnt/d/lab/sample.dotblog/Secrets Manager/Lab.AI.Secret/blog.md`。
- [x] **步驟 3：清理暫存檔案**
  - **說明**：刪除工具 output 目錄下的暫存 Markdown 檔案，保持環境乾淨。
