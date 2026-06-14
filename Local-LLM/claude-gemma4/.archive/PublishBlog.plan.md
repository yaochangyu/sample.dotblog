# 實作計畫：PublishBlog - 使用 metablog-cli 部署 blog.md

本計畫旨在透過 MetaWeblog XML-RPC API，將 `blog.md` 部署（更新）至點部落。

## 實作步驟

- [x] **步驟 1：執行 metablog-cli 部署命令** <!-- id: 1 -->
  - **說明**：以 `Cwd=/home/yao/.gemini/skills/metablog-cli` 執行以下指令部署：
    `env BLOG_API_URL="https://dotblogs.com.tw/Api/MetaWeblog" BLOG_USER="yaochang.yu@gmail.com" BLOG_PASSWORD="$BLOG_PASSWORD" uv run scripts/metablog_cli.py publish /mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/blog.md`
    由於 `blog.md` 內已包含 `postId` (a89d1373-a7d8-45fb-9a31-011f6d565469), 此操作將會更新既有文章。

- [x] **步驟 2：確認發布狀態** <!-- id: 2 -->
  - **說明**：檢查發布指令的輸出，確認文章已成功更新。

- [x] **步驟 3：更新專案結構檔 `@tree.md`** <!-- id: 3 -->
  - **說明**：將 `PublishBlog.plan.md` 新增至 `@tree.md` 結構中。

- [x] **步驟 4：封存計畫檔案** <!-- id: 4 -->
  - **說明**：完成所有步驟後，將 `PublishBlog.plan.md` 移動至 `.archive` 資料夾，並更新 `@tree.md`。
