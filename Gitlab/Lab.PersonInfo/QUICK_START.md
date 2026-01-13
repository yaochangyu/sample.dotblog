# 🚀 快速開始指南

本指南協助您快速設定並執行 GitLab 開發者分析系統。

---

## 📋 前置需求

- Python 3.10+
- uv 套件管理工具
- GitLab Personal Access Token
- Git Repository（本地）

---

## ⚡ 5 分鐘快速設定

### 1. 安裝 uv

```bash
# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh

# Windows
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"
```

### 2. 安裝依賴

```bash
cd Lab.PersonInfo
uv sync
```

### 3. 設定 GitLab Token

```bash
# 複製環境變數範本
cp .env.example .env

# 編輯 .env 並填入您的 GitLab Token
nano .env
```

在 `.env` 中填入：
```bash
GITLAB_URL=https://gitlab.com
GITLAB_TOKEN=glpat-your_token_here
```

**取得 Token**：前往 GitLab → Settings → Access Tokens，勾選：
- `read_api`
- `read_repository`
- `read_user`

### 4. 測試連線

```bash
uv run python scripts/test_connection.py
```

預期輸出：
```
✅ 認證成功！使用者: your_username
```

---

## 🎯 執行分析

### 方式 A：完整分析流程（推薦）

```bash
# 1. 收集 GitLab 數據（過去一年）
uv run python scripts/collect_data.py

# 2. 收集本地 Git 數據（如果有本地 repo）
cd /path/to/your/repo
uv run python /path/to/Lab.PersonInfo/scripts/collectors/git_local_collector.py

# 3. 執行分析
cd /path/to/Lab.PersonInfo
uv run python scripts/main.py analyze-all
```

### 方式 B：只使用已收集的數據

如果您已經有 `scripts/output/raw/` 目錄下的數據檔案：

```bash
# 直接執行分析
uv run python scripts/main.py analyze-all
```

---

## 📊 查看結果

分析完成後，結果會儲存在 `scripts/output/processed/`：

```
scripts/output/processed/
├── final_scores.csv              # 最終評分與排名
├── commit_quality_scores.csv     # Commit 品質評分
├── contribution_scores.csv       # 貢獻量評分
├── tech_breadth_scores.csv       # 技術廣度評分
└── unified_developers.csv        # 開發者身份映射
```

### 使用 Excel/LibreOffice 開啟

```bash
# macOS
open scripts/output/processed/final_scores.csv

# Linux
xdg-open scripts/output/processed/final_scores.csv

# Windows
start scripts/output/processed/final_scores.csv
```

---

## 📈 結果解讀

### final_scores.csv 欄位說明

| 欄位 | 說明 | 範圍 |
|------|------|------|
| name | 開發者姓名 | - |
| email | 開發者 Email | - |
| final_score | 最終評分 | 0-10 |
| grade | 等級 | 🏆高級/⭐中級/🌱初級 |
| commit_quality_score | Commit 品質分數 | 0-10 |
| contribution_score | 貢獻量分數 | 0-10 |
| tech_breadth_score | 技術廣度分數 | 0-10 |

### 評分標準

- **🏆 高級工程師** (8.0-10.0 分)
  - Commit Message 規範率 >80%
  - 小型變更佔比 >60%
  - 涉及 3+ 種技術棧

- **⭐ 中級工程師** (5.0-7.9 分)
  - Commit Message 規範率 60-80%
  - 變更粒度合理
  - 涉及 2-3 種技術棧

- **🌱 初級工程師** (0.0-4.9 分)
  - Commit Message 不規範
  - 單一技術棧
  - 需改進空間大

---

## 🔧 常見問題

### Q1: 沒有 GitLab Token 怎麼辦？

**A**: 可以只使用 Git 本地數據進行分析：

```bash
# 在您的 Git Repository 中執行
cd /path/to/your/repo
uv run python /path/to/Lab.PersonInfo/scripts/collectors/git_local_collector.py

# 然後執行分析
cd /path/to/Lab.PersonInfo
uv run python scripts/main.py analyze-all
```

### Q2: 分析結果為空？

**A**: 檢查以下事項：
1. `scripts/output/raw/` 是否有數據檔案？
2. 數據檔案是否有內容（不是空檔案）？
3. 開發者列表是否為空？

```bash
# 檢查數據檔案
ls -lh scripts/output/raw/

# 檢查開發者數量
head scripts/output/raw/git_developers.csv
```

### Q3: 如何只分析特定時間範圍？

**A**: 收集數據時指定時間範圍：

```bash
uv run python scripts/collect_data.py \
  --from 2024-01-01 \
  --to 2024-12-31
```

### Q4: 如何調整評分標準？

**A**: 編輯 `scripts/config/analysis_config.py`，參考 `CONFIG_GUIDE.md`。

---

## 📚 進階使用

### 自訂開發者映射

如果同一開發者有多個 Email，可建立手動映射：

```python
# 編輯 scripts/main.py，在 analyze_all 函數中加入：
manual_mapping = {
    "old.email@example.com": "current.email@example.com",
    "another.old@example.com": "current.email@example.com",
}
merger.process_all(manual_developer_mapping=manual_mapping)
```

### 只收集特定專案

```bash
# 先取得專案 ID
uv run python scripts/collect_data.py --only-projects

# 然後只收集特定專案的數據
uv run python scripts/collect_data.py --projects 12345,67890
```

---

## 🆘 需要協助？

- 📖 查看完整文檔：`README.md`
- 🔧 環境設定問題：`SETUP.md`
- 📡 API 使用問題：`API_USAGE.md`
- ⚙️ 配置調整：`CONFIG_GUIDE.md`
- 📊 評估標準：`analysis-spec.md`

---

## ✅ 下一步

系統已經可以運作！您可以：

1. ✅ 執行完整分析，查看團隊評分
2. ✅ 根據結果調整評分標準（`CONFIG_GUIDE.md`）
3. ✅ 定期執行分析，追蹤進步趨勢
4. ✅ 擴展功能（Code Review 分析、視覺化報告等）

---

**版本**：v1.0
**最後更新**：2026-01-13
