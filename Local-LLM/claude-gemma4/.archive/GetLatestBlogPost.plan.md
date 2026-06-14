# 實作計畫：GetLatestBlogPost - 從點部落下載最新一筆文章

本計畫旨在透過 metablog-cli 與 MetaWeblog API，從點部落下載最新一筆文章為 Markdown 存檔。

## 實作步驟

- [x] **步驟 1：執行 metablog-cli 取得一筆文章命令** <!-- id: 1 -->
  - **說明**：以 `Cwd=/home/yao/.gemini/config/skills/metablog-cli` 執行：
    `env BLOG_API_URL="https://dotblogs.com.tw/Api/MetaWeblog" BLOG_USER="yaochang.yu@gmail.com" BLOG_PASSWORD="$BLOG_PASSWORD" uv run scripts/metablog_cli.py get --latest 1`
    此命令能直接傳入環境變數，避開非互動式 shell 無法自動載入 `.bashrc` 的問題，並下載最新的一筆文章。

- [x] **步驟 2：確認下載檔案成果** <!-- id: 2 -->
  - **說明**：檢查輸出檔案，確認其成功存放在 `output/` 資料夾下，且 Markdown 內容與 frontmatter 均完整無誤。

- [x] **步驟 3：更新專案結構檔 `@tree.md`** <!-- id: 3 -->
  - **說明**：將 `GetLatestBlogPost.plan.md` 與下載的 Markdown 檔案加入專案結構圖。

- [x] **步驟 4：封存計畫檔案** <!-- id: 4 -->
  - **說明**：完成所有步驟後，將 `GetLatestBlogPost.plan.md` 移動至 `.archive` 資料夾，並更新 `@tree.md`。
