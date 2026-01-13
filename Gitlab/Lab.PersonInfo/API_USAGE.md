# 📡 GitLab API 數據收集使用指南

本文件說明如何使用 GitLab API 收集器收集開發者數據。

---

## 🚀 快速開始

### 1. 收集所有數據（過去一年）

```bash
uv run python scripts/collect_data.py
```

這會自動收集：
- ✅ 所有可訪問的專案列表
- ✅ Merge Request 數據
- ✅ Review Comments 數據
- ✅ Commit 數據

### 2. 收集指定時間範圍的數據

```bash
uv run python scripts/collect_data.py \
  --from 2024-01-01 \
  --to 2024-12-31
```

### 3. 只收集特定專案的數據

```bash
# 先取得專案 ID
uv run python scripts/collect_data.py --only-projects

# 然後收集指定專案的數據
uv run python scripts/collect_data.py --projects 12345,67890
```

---

## 📊 輸出檔案說明

所有檔案會儲存在 `scripts/output/raw/` 目錄：

### 1. `gitlab_projects.csv` - 專案列表

| 欄位 | 說明 |
|------|------|
| project_id | 專案 ID（唯一識別） |
| name | 專案名稱 |
| path_with_namespace | 完整路徑（含群組） |
| description | 專案描述 |
| visibility | 可見性（public/internal/private） |
| created_at | 建立時間 |
| last_activity_at | 最後活動時間 |
| web_url | 網頁 URL |
| default_branch | 預設分支名稱 |
| archived | 是否已封存 |

### 2. `gitlab_merge_requests.csv` - MR 數據

| 欄位 | 說明 |
|------|------|
| project_id | 所屬專案 ID |
| mr_iid | MR 編號（專案內唯一） |
| mr_id | MR ID（全域唯一） |
| title | MR 標題 |
| description | MR 描述 |
| state | 狀態（opened/merged/closed） |
| created_at | 建立時間 |
| merged_at | 合併時間 |
| author_username | 作者 username |
| author_name | 作者姓名 |
| assignee_usernames | Assignee（逗號分隔） |
| reviewer_usernames | Reviewer（逗號分隔） |
| additions | 新增行數 |
| deletions | 刪除行數 |
| changed_files | 變更檔案數 |
| user_notes_count | 評論數量 |
| labels | 標籤（逗號分隔） |

### 3. `gitlab_review_comments.csv` - Review Comments

| 欄位 | 說明 |
|------|------|
| project_id | 所屬專案 ID |
| mr_iid | 所屬 MR 編號 |
| note_id | Comment ID |
| author_username | 作者 username |
| author_name | 作者姓名 |
| body | 評論內容 |
| created_at | 建立時間 |
| resolvable | 是否可解決 |
| resolved | 是否已解決 |
| diff_file_path | 相關檔案路徑 |
| diff_line | 相關行號 |

### 4. `gitlab_commits.csv` - Commit 數據

| 欄位 | 說明 |
|------|------|
| project_id | 所屬專案 ID |
| commit_sha | Commit SHA（完整） |
| short_id | Commit SHA（短版） |
| title | Commit 標題 |
| message | Commit 完整訊息 |
| author_name | 作者姓名 |
| author_email | 作者 Email |
| authored_date | Commit 時間 |
| committer_name | Committer 姓名 |
| committer_email | Committer Email |
| committed_date | Committed 時間 |
| additions | 新增行數 |
| deletions | 刪除行數 |
| total | 總變更行數 |
| parent_ids | 父 Commit（逗號分隔） |

---

## 🎯 進階使用

### 只收集特定類型的數據

```bash
# 只收集專案列表
uv run python scripts/collect_data.py --only-projects

# 只收集 MR 數據
uv run python scripts/collect_data.py --only-mr --from 2024-01-01 --to 2024-12-31

# 只收集 Review Comments
uv run python scripts/collect_data.py --only-comments --from 2024-01-01 --to 2024-12-31

# 只收集 Commits
uv run python scripts/collect_data.py --only-commits --from 2024-01-01 --to 2024-12-31
```

### 在 Python 中使用 API 收集器

```python
from collectors.gitlab_api_collector import GitLabAPICollector

# 建立收集器
collector = GitLabAPICollector()

# 收集專案列表
projects_df = collector.collect_projects()
print(f"收集到 {len(projects_df)} 個專案")

# 收集特定專案的 MR 數據
project_ids = [12345, 67890]
mr_df = collector.collect_merge_requests(
    project_ids=project_ids,
    start_date="2024-01-01",
    end_date="2024-12-31"
)

# 收集 Review Comments
comments_df = collector.collect_review_comments(
    project_ids=project_ids,
    start_date="2024-01-01",
    end_date="2024-12-31"
)

# 一次收集所有數據
results = collector.collect_all(
    project_ids=project_ids,
    start_date="2024-01-01",
    end_date="2024-12-31"
)
```

---

## ⚙️ 配置選項

### 調整 API 請求間隔

編輯 `.env` 檔案：

```bash
# 預設為 0.3 秒
API_REQUEST_DELAY=0.3

# 如果遇到 Rate Limiting，可增加間隔
API_REQUEST_DELAY=0.5
```

### 調整錯誤重試次數

編輯 `.env` 檔案：

```bash
# 預設為 3 次
API_MAX_RETRIES=3

# 網路不穩定時可增加
API_MAX_RETRIES=5
```

---

## ⚠️ 常見問題

### Q1: 429 Too Many Requests（請求過於頻繁）

**原因**：觸發 GitLab API Rate Limiting

**解決方法**：

1. 增加請求間隔：
   ```bash
   export API_REQUEST_DELAY=0.5
   ```

2. 分批收集數據：
   ```bash
   # 先收集 MR
   uv run python scripts/collect_data.py --only-mr --from 2024-01-01 --to 2024-06-30

   # 再收集 Comments
   uv run python scripts/collect_data.py --only-comments --from 2024-01-01 --to 2024-06-30
   ```

3. 使用 GitLab Ultimate 版本（更高的 Rate Limit）

### Q2: 收集數據花費時間過長

**原因**：專案數量過多或時間範圍過大

**解決方法**：

1. 縮小時間範圍：
   ```bash
   # 只收集最近 3 個月
   uv run python scripts/collect_data.py --from 2024-10-01 --to 2024-12-31
   ```

2. 只收集特定專案：
   ```bash
   uv run python scripts/collect_data.py --projects 12345,67890
   ```

3. 分批收集：
   ```bash
   # Q1
   uv run python scripts/collect_data.py --from 2024-01-01 --to 2024-03-31
   # Q2
   uv run python scripts/collect_data.py --from 2024-04-01 --to 2024-06-30
   ```

### Q3: 無法存取某些專案

**原因**：Token 權限不足或專案已被刪除/封存

**解決方法**：

1. 確認 Token 權限包含 `read_api`, `read_repository`
2. 檢查是否有專案存取權限
3. 排除已封存的專案：
   ```python
   collector.collect_projects(include_archived=False)
   ```

### Q4: MR 的 Diff 統計資訊不準確

**原因**：GitLab API 對於大型 MR 可能會限制回傳的 Diff 資訊

**解決方法**：

- 使用 Git 本地數據收集器補充（步驟 5）
- 直接使用 `git log --stat` 取得更精確的統計

### Q5: Review Comments 數量比預期少

**原因**：

1. GitLab 的 Notes API 只回傳非系統訊息
2. 某些舊版 GitLab 可能沒有 Reviewers 功能

**解決方法**：

- 確認 GitLab 版本支援 Reviewers 功能（14.0+）
- 檢查 `user_notes_count` 欄位與實際收集的 Comments 數量是否一致

---

## 📈 效能優化建議

### 1. 使用專案過濾

```python
# 只收集有活動的專案（最近 6 個月）
from datetime import datetime, timedelta

collector = GitLabAPICollector()
projects_df = collector.collect_projects()

# 過濾最近有活動的專案
cutoff_date = datetime.now() - timedelta(days=180)
active_projects = projects_df[
    pd.to_datetime(projects_df["last_activity_at"]) > cutoff_date
]

project_ids = active_projects["project_id"].tolist()
```

### 2. 使用本地快取

```python
import os

# 如果已有專案列表，直接使用
if os.path.exists("scripts/output/raw/gitlab_projects.csv"):
    projects_df = pd.read_csv("scripts/output/raw/gitlab_projects.csv")
    project_ids = projects_df["project_id"].tolist()
else:
    projects_df = collector.collect_projects()
    project_ids = projects_df["project_id"].tolist()
```

### 3. 並行處理（進階）

```python
from concurrent.futures import ThreadPoolExecutor

def collect_project_data(project_id):
    collector = GitLabAPICollector()
    return collector.collect_merge_requests(
        project_ids=[project_id],
        start_date="2024-01-01",
        end_date="2024-12-31"
    )

# 同時處理多個專案（注意 Rate Limiting）
with ThreadPoolExecutor(max_workers=3) as executor:
    results = list(executor.map(collect_project_data, project_ids))
```

⚠️ **注意**：並行處理可能會更快觸發 Rate Limiting，請謹慎使用。

---

## 📊 數據驗證

收集完數據後，建議進行驗證：

```python
import pandas as pd

# 讀取收集的數據
projects_df = pd.read_csv("scripts/output/raw/gitlab_projects.csv")
mr_df = pd.read_csv("scripts/output/raw/gitlab_merge_requests.csv")
comments_df = pd.read_csv("scripts/output/raw/gitlab_review_comments.csv")
commits_df = pd.read_csv("scripts/output/raw/gitlab_commits.csv")

print(f"專案數量: {len(projects_df)}")
print(f"MR 數量: {len(mr_df)}")
print(f"Comments 數量: {len(comments_df)}")
print(f"Commits 數量: {len(commits_df)}")

# 檢查日期範圍
print(f"\nMR 日期範圍: {mr_df['created_at'].min()} ~ {mr_df['created_at'].max()}")
print(f"Commits 日期範圍: {commits_df['authored_date'].min()} ~ {commits_df['authored_date'].max()}")

# 檢查是否有缺失值
print(f"\nMR 缺失值:\n{mr_df.isnull().sum()}")
```

---

## 🔄 增量更新（未來功能）

目前版本使用全量收集，未來版本將支援增量更新：

```bash
# 只收集最近 7 天的新數據
uv run python scripts/collect_data.py --incremental --days 7
```

---

**文件版本**：v1.0
**最後更新**：2026-01-13
