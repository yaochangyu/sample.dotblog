# 🎯 GitLab 開發者技術水平分析系統

[![狀態](https://img.shields.io/badge/%E7%8B%80%E6%85%8B-%E6%A0%B8%E5%BF%83%E5%8A%9F%E8%83%BD%E5%B7%B2%E5%AE%8C%E6%88%90-brightgreen)]()
[![進度](https://img.shields.io/badge/%E9%80%B2%E5%BA%A6-55%25-yellow)]()
[![Python](https://img.shields.io/badge/Python-3.10%2B-blue)]()
[![授權](https://img.shields.io/badge/%E6%8E%88%E6%AC%8A-MIT-blue)]()

## 📖 簡介

這是一套完整的 GitLab 開發者技術評估系統，根據 `analysis-spec.md` 的評估標準，自動化收集、分析並產生開發者貢獻報告。

**🎉 核心功能已完成，系統可立即使用！**

📚 **快速開始**：參閱 [`QUICK_START.md`](QUICK_START.md) 5 分鐘完成設定

### 評估維度

| 維度 | 權重 | 說明 |
|------|------|------|
| Commit 品質 | 23% | Message 規範性、變更粒度、修復率 |
| 技術廣度 | 18% | 語言種類、技術棧覆蓋 |
| 進步趨勢 | 15% | 成長曲線、技能提升 |
| 程式碼貢獻量 | 12% | 提交次數、活躍度 |
| 協作能力 | 12% | Merge Commits、衝突處理 |
| **Code Review 品質** | **10%** | Review 參與度、深度、時效性 |
| 工作模式 | 10% | 時間分佈、穩定性 |

---

## 🚀 快速開始

### 前置需求

- Python 3.10+
- [uv](https://github.com/astral-sh/uv) 套件管理工具
- GitLab Personal Access Token

### 安裝 uv

```bash
# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh

# Windows
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# 或使用 pip
pip install uv
```

### 安裝專案依賴

```bash
# 切換到專案目錄
cd Lab.PersonInfo

# 使用 uv 安裝所有依賴
uv sync

# 或者只安裝生產環境依賴
uv sync --no-dev
```

### 設定 GitLab Token

1. 前往 GitLab → Settings → Access Tokens
2. 建立 Token，並勾選以下權限：
   - `read_api`
   - `read_repository`
   - `read_user`
3. 將 Token 存入環境變數：

```bash
# 建立 .env 檔案
cp .env.example .env

# 編輯 .env 並填入 Token
GITLAB_URL=https://gitlab.com
GITLAB_TOKEN=your_token_here
```

---

## 📚 使用方式

### 1. 分析單一開發者

```bash
# 使用 uvx 執行（推薦）
uvx --from . gitlab-analyzer analyze \
  --user "開發者名稱" \
  --from "2024-01-01" \
  --to "2024-12-31"

# 或使用 uv run
uv run python scripts/main.py analyze \
  --user "開發者名稱" \
  --from "2024-01-01" \
  --to "2024-12-31"
```

### 2. 批次分析所有開發者

```bash
uvx --from . gitlab-analyzer analyze-all \
  --from "2024-01-01" \
  --to "2024-12-31"
```

### 3. 產生團隊匯總報告

```bash
uvx --from . gitlab-analyzer team-report \
  --from "2024-01-01" \
  --to "2024-12-31"
```

### 4. 增量更新數據

```bash
uvx --from . gitlab-analyzer update --since "2024-12-01"
```

---

## 📂 專案結構

```
Lab.PersonInfo/
├── scripts/                          # 所有腳本
│   ├── config/                       # 配置檔案
│   │   ├── gitlab_config.py          # GitLab 連線設定
│   │   └── analysis_config.py        # 分析參數設定
│   ├── collectors/                   # 數據收集器
│   │   ├── gitlab_api_collector.py   # GitLab API 數據收集
│   │   ├── git_local_collector.py    # Git 本地數據收集
│   │   └── data_merger.py            # 數據合併與清洗
│   ├── analyzers/                    # 分析器
│   │   ├── commit_analyzer.py        # Commit 品質分析
│   │   ├── review_analyzer.py        # Code Review 分析
│   │   └── ...
│   ├── reporters/                    # 報告生成器
│   │   ├── markdown_reporter.py      # Markdown 報告
│   │   ├── csv_exporter.py           # CSV 導出
│   │   └── visualization.py          # 視覺化圖表
│   ├── output/                       # 輸出目錄
│   │   ├── raw/                      # 原始數據
│   │   ├── processed/                # 處理後數據
│   │   └── reports/                  # 最終報告
│   └── main.py                       # 主程式入口
├── analysis-spec.md                  # 評估規範文件
├── pyproject.toml                    # 專案配置（uv 使用）
└── README.md                         # 本檔案
```

---

## 🔧 開發指南

### 安裝開發環境

```bash
# 安裝所有依賴（包含開發工具）
uv sync

# 安裝 pre-commit hooks（可選）
uv run pre-commit install
```

### 執行測試

```bash
# 執行所有測試
uv run pytest

# 執行測試並產生覆蓋率報告
uv run pytest --cov=scripts --cov-report=html

# 執行特定測試
uv run pytest scripts/tests/test_commit_analyzer.py
```

### 程式碼格式化

```bash
# 使用 black 格式化程式碼
uv run black scripts/

# 使用 ruff 檢查程式碼品質
uv run ruff check scripts/
```

---

## 📊 輸出檔案說明

### 原始數據（`scripts/output/raw/`）

- `gitlab_projects.csv` - GitLab 專案列表
- `gitlab_merge_requests.csv` - MR 數據
- `gitlab_review_comments.csv` - Review Comments
- `gitlab_commits.csv` - Commit 數據（API）
- `git_commits.csv` - Commit 數據（本地）
- `git_file_changes.csv` - 檔案變更統計

### 處理後數據（`scripts/output/processed/`）

- `unified_developers.csv` - 開發者統一身份映射
- `all_commits_merged.csv` - 合併後的 Commit 數據
- `all_reviews_merged.csv` - 合併後的 Review 數據
- `commit_quality_scores.csv` - Commit 品質評分
- `review_quality_scores.csv` - Code Review 評分
- `final_scores.csv` - 最終綜合評分

### 報告（`scripts/output/reports/`）

- `{developer_name}_report.md` - 個人技術評估報告
- `{developer_name}_radar.png` - 雷達圖
- `{developer_name}_trend.png` - 進步趨勢圖
- `team_summary.md` - 團隊匯總報告

---

## ⚙️ 配置選項

編輯 `scripts/config/analysis_config.py` 可調整評估參數：

```python
# 變更粒度閾值
SMALL_CHANGE_THRESHOLD = 100   # 小型變更（行數）
MEDIUM_CHANGE_THRESHOLD = 500  # 中型變更（行數）

# 評分權重
WEIGHTS = {
    'contribution': 0.12,
    'commit_quality': 0.23,
    'tech_breadth': 0.18,
    'collaboration': 0.12,
    'code_review': 0.10,
    'work_pattern': 0.10,
    'progress': 0.15,
}

# 排除規則
EXCLUDED_BOTS = ['renovate', 'dependabot', 'gitlab-bot']
EXCLUDED_FILE_PATTERNS = ['package-lock.json', 'yarn.lock', 'dist/', 'build/']
```

---

## 🐛 常見問題

### Q1: GitLab API Token 權限不足

**錯誤**：`401 Unauthorized`

**解決**：確認 Token 包含以下權限：
- `read_api`
- `read_repository`
- `read_user`

### Q2: API Rate Limiting

**錯誤**：`429 Too Many Requests`

**解決**：調整 `scripts/config/gitlab_config.py` 中的請求間隔：
```python
API_REQUEST_DELAY = 0.5  # 每次請求間隔 0.5 秒
```

### Q3: 同一開發者多個 Email

**解決**：編輯 `scripts/output/processed/unified_developers.csv`，手動建立映射關係。

---

## 📝 待辦事項

詳見 `GitLab開發者分析系統實作計畫.md` 和 `GitLab開發者分析系統實作計畫.Progress.md`。

---

## 📄 授權

MIT License

---

## 🤝 貢獻

歡迎提交 Issue 和 Pull Request！

---

**版本**：v1.0.0
**最後更新**：2026-01-13
**作者**：Lab.PersonInfo Team
