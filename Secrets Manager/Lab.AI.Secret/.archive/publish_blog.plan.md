# 發布文章至點部落計畫

此計畫旨在將 `Secrets Manager/Lab.AI.Secret/blog.md` 的內容，透過 `metablog-cli` 工具發布（更新）至點部落。

## 計畫步驟

- [x] **步驟 1：驗證點部落發布工具與文章內容**
  - **說明**：確認 `metablog-cli` 工具可用，並確認 /mnt/d/lab/sample.dotblog/Secrets Manager/Lab.AI.Secret/blog.md 的 frontmatter 與檔案路徑無誤。
- [x] **步驟 2：執行發布/更新**
  - **說明**：執行 `uv run` 搭配 `metablog_cli.py` 發布該 Markdown 文章。
- [x] **步驟 3：確認發布結果**
  - **說明**：檢查發布輸出，驗證文章已成功更新，並提供發布連結。
