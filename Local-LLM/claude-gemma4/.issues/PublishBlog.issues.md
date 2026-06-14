# 問題紀錄：PublishBlog

## 失敗的方法
在工作目錄下直接執行 `uv run /home/yao/.gemini/config/skills/metablog-cli/scripts/metablog_cli.py`。

## 步驟
1. 執行命令：`uv run /home/yao/.gemini/config/skills/metablog-cli/scripts/metablog_cli.py publish /mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/blog.md`，且工作目錄 `Cwd` 設為 `/mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4`。

## 原因
`uv` 找不到 `metablog-cli` 專案的 `pyproject.toml` 與依賴設定，導致 `ModuleNotFoundError: No module named 'markdown'` 錯誤。

## 失敗的方法 2
切換工作目錄至 `/home/yao/.gemini/config/skills/metablog-cli` 後執行。

## 步驟
1. 執行命令：`uv run scripts/metablog_cli.py publish /mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/blog.md`，工作目錄 `Cwd` 為 `/home/yao/.gemini/config/skills/metablog-cli`。

## 原因
執行時發生 `OSError: unsupported XML-RPC protocol` 錯誤。由於專案中缺少 `.env` 設定檔，且全域憑證路徑 `~/.claude/creds/.creds` 亦不存在，導致 `BLOG_API_URL` 等環境變數為空，無法初始化 XML-RPC 用戶端。

## 後續對策
先詢問使用者有關點部落的 API URL、帳號、密碼等憑證資訊，或請使用者提供 `~/.claude/creds/.creds` 檔案，再進行部署。
