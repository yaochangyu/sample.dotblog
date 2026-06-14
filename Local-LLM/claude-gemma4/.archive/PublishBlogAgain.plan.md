# 實作計畫：PublishBlogAgain - 再次部署 blog.md 到點部落

本計畫旨在再次執行 `metablog-cli` 工具，將 `blog.md` 部署至點部落。

## 實作步驟

- [x] **步驟 1：執行 metablog-cli 部署命令** <!-- id: 1 -->
  - **說明**：以 `Cwd=/home/yao/.gemini/skills/metablog-cli` 執行以下指令：
    `env BLOG_API_URL="https://dotblogs.com.tw/Api/MetaWeblog" BLOG_USER="yaochang.yu@gmail.com" BLOG_PASSWORD="$BLOG_PASSWORD" uv run scripts/metablog_cli.py publish /mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/blog.md`
    利用已驗證的環境變數進行更新既有文章。

- [x] **步驟 2：確認部署成果** <!-- id: 2 -->
  - **說明**：確認指令輸出成功（ok=True），並取得點部落後台編輯連結。

- [x] **步驟 3：更新專案結構檔 `@tree.md`** <!-- id: 3 -->
  - **說明**：將 `PublishBlogAgain.plan.md` 加入專案結構圖。

- [x] **步驟 4：封存計畫檔案** <!-- id: 4 -->
  - **說明**：完成所有步驟後，將 `PublishBlogAgain.plan.md` 移動至 `.archive` 資料夾，並更新 `@tree.md`。
