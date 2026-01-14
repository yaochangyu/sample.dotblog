# GitLab 開發者程式碼品質分析工具

## 功能說明

這個工具提供三種使用模式：

### 模式 1: 全體開發者分析 (`gitlab_collector.py`)

收集所有開發者的資料，用於團隊整體分析和比較。

**輸出檔案：**
1. **Commit Log** (`all-user.commits.csv`)
   - 提交 ID、作者、時間
   - 程式碼增刪行數
   - Commit 訊息

2. **程式碼異動** (`all-user.code-changes.csv`)
   - 異動的檔案路徑
   - 新增、刪除、重新命名的檔案
   - Diff 內容

3. **Code Review 資訊** (`all-user.merge-requests.csv`)
   - Merge Request 狀態
   - 審查者、核准者
   - 討論和評論數量
   - 合併狀態

4. **統計資訊** (`all-user.statistics.csv`)
   - 每位開發者的總 commit 數
   - 程式碼增刪總量
   - 參與的專案數
   - 平均每次 commit 的程式碼變更量

### 模式 2: 特定開發者深度分析 (`gitlab_developer_collector.py`)

針對單一開發者進行詳細分析，提供更多細節資訊。

**輸出檔案：**
1. **Commit 詳細記錄** (`{developer}.commits.csv`)
   - 所有 commit 的完整資訊
   - 包含 committer 資訊
   - 專案完整路徑

2. **程式碼異動詳情** (`{developer}.code-changes.csv`)
   - 每個檔案的變更詳情
   - 檔案類型統計
   - Diff 內容（前 2000 字元）

3. **Merge Request 資訊** (`{developer}.merge-requests.csv`)
   - 創建的 MR 完整資訊
   - 包含標籤、里程碑
   - 審查者、指派者資訊
   - 評論詳情

4. **Code Review 參與** (`{developer}.code-reviews.csv`)
   - 參與審查的 MR 列表
   - 審查時間和狀態

5. **統計摘要** (`{developer}.statistics.csv`)
   - 完整的統計指標
   - 檔案類型分析
   - MR 合併率

6. **分析報告** (`{developer}.report.txt`)
   - 易讀的文字報告
   - 關鍵指標摘要

### 模式 3: 程式化查詢 API (Python 模組)

`GitLabCollector` 類別提供了可供其他 Python 程式呼叫的查詢方法，適合整合到自己的自動化工具或分析腳本中。

**可用的查詢方法：**

#### 1. 取得專案和使用者列表
```python
from gitlab_collector import GitLabCollector

collector = GitLabCollector()

# 取得所有專案
projects = collector.get_projects_list()
# 返回: List[Dict] - 包含 id, name, description, web_url, created_at 等

# 取得所有使用者/帳號
users = collector.get_all_users()
# 返回: List[Dict] - 包含 id, username, name, email, state, created_at 等
```

#### 2. 查詢特定使用者在特定專案的資料
```python
# 取得 commit 記錄
commits = collector.get_user_commits_in_project(
    project_id=123,
    user_email="user@example.com"  # 或使用 user_name="John Doe"
)
# 返回: List[Dict] - 詳細的 commit 資訊，包含變更檔案列表

# 取得程式碼異動詳情
code_changes = collector.get_user_code_changes_in_project(
    project_id=123,
    user_email="user@example.com"
)
# 返回: List[Dict] - 每個檔案的變更內容、新增/刪除行數、diff 內容

# 取得 Code Review (MR) 資訊
merge_requests = collector.get_user_merge_requests_in_project(
    project_id=123,
    user_username="johndoe"  # 或使用 user_name="John Doe"
)
# 返回: List[Dict] - MR 詳情，包含討論、審查者、變更檔案等

# 取得統計資訊
statistics = collector.get_user_statistics_in_project(
    project_id=123,
    user_email="user@example.com",
    user_username="johndoe"
)
# 返回: Dict - 完整統計資料，包含 commits、files、merge_requests、productivity 等指標
```

#### 3. 自訂時間範圍
```python
# 初始化時指定時間範圍
collector = GitLabCollector(
    start_date="2024-01-01",
    end_date="2024-12-31"
)
```

**返回的詳細資料包含：**

- **專案資訊**: id, name, path, web_url, default_branch, star_count, forks_count 等
- **使用者資訊**: id, username, name, email, state, created_at, last_activity_on 等
- **Commit 資訊**: commit_id, author, committed_date, additions, deletions, total_changes, changed_files 等
- **程式碼異動**: file_path, new_file, deleted_file, renamed_file, added_lines, removed_lines, diff_content 等
- **MR 資訊**: title, state, merged, assignees, reviewers, comments, changed_files, commits, time_stats 等
- **統計資料**: 
  - Commits: 總數、新增/刪除行數、平均變更量
  - Files: 異動檔案數、檔案類型分布、最常修改的檔案
  - MR: 創建/合併數、合併率、評論數
  - Productivity: 每日 commit 數、每日變更量、活躍天數

## 安裝步驟

### 1. 安裝 uv (如果尚未安裝)

```bash
# Windows (PowerShell)
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### 2. 使用 uv 安裝相依套件

```bash
cd scripts
uv sync
```

### 3. 設定 GitLab Token

1. 登入您的 GitLab
2. 前往 **User Settings** > **Access Tokens**
3. 建立新的 Personal Access Token，權限需包含：
   - `read_api`
   - `read_repository`
4. 複製產生的 Token

### 4. 設定配置檔

編輯 `config.py`，填入您的資訊：

```python
GITLAB_URL = "https://gitlab.com"  # 或您的 GitLab 伺服器網址
GITLAB_TOKEN = "YOUR_TOKEN_HERE"   # 貼上您的 Access Token

START_DATE = "2024-01-01"  # 分析起始日期
END_DATE = "2024-12-31"    # 分析結束日期
```

## 使用方式

### A. 全體開發者分析

#### 方式 1：使用 uv run
```bash
cd scripts
uv run gitlab_collector.py
```

#### 方式 2：背景執行（推薦用於大量資料）
```bash
cd scripts
nohup uv run gitlab_collector.py > collector.log 2>&1 &

# 監控進度
tail -f collector.log | grep "處理專案:"
```

#### 方式 3：進入虛擬環境
```bash
cd scripts
uv sync
source .venv/bin/activate  # Linux/macOS
# 或
.venv\Scripts\activate  # Windows

python gitlab_collector.py
```

### B. 特定開發者分析

#### 使用 Email 分析
```bash
cd scripts
uv run gitlab_developer_collector.py user@example.com
```

#### 使用 Username 分析
```bash
cd scripts
uv run gitlab_developer_collector.py johndoe
```

#### 互動式輸入
```bash
cd scripts
uv run gitlab_developer_collector.py

# 程式會提示輸入 Email 或 Username
```

#### 背景執行
```bash
cd scripts
nohup uv run gitlab_developer_collector.py user@example.com > developer.log 2>&1 &
```

### C. 程式化查詢（Python API）

#### 範例 1: 取得所有專案和使用者
```python
from gitlab_collector import GitLabCollector

# 初始化
collector = GitLabCollector(
    start_date="2024-01-01",
    end_date="2024-12-31"
)

# 取得所有專案
projects = collector.get_projects_list()
for project in projects:
    print(f"{project['name']}: {project['web_url']}")

# 取得所有使用者
users = collector.get_all_users()
for user in users:
    print(f"{user['name']} ({user['username']}): {user['email']}")
```

#### 範例 2: 查詢特定使用者在特定專案的資料
```python
from gitlab_collector import GitLabCollector
import json

collector = GitLabCollector()

project_id = 123
user_email = "user@example.com"
user_username = "johndoe"

# 取得 commits
commits = collector.get_user_commits_in_project(project_id, user_email=user_email)
print(f"找到 {len(commits)} 筆 commits")

# 取得程式碼異動
code_changes = collector.get_user_code_changes_in_project(project_id, user_email=user_email)
print(f"找到 {len(code_changes)} 筆程式碼異動")

# 取得 MR
mrs = collector.get_user_merge_requests_in_project(project_id, user_username=user_username)
print(f"找到 {len(mrs)} 筆 Merge Requests")

# 取得統計資訊
stats = collector.get_user_statistics_in_project(
    project_id, 
    user_email=user_email,
    user_username=user_username
)
print(json.dumps(stats, indent=2, ensure_ascii=False))
```

#### 範例 3: 批次查詢多個專案
```python
from gitlab_collector import GitLabCollector

collector = GitLabCollector()

# 取得專案列表
projects = collector.get_projects_list()
user_email = "user@example.com"

# 遍歷所有專案，收集該使用者的資料
all_commits = []
for project in projects:
    commits = collector.get_user_commits_in_project(
        project['id'], 
        user_email=user_email
    )
    all_commits.extend(commits)

print(f"使用者在所有專案的總 commit 數: {len(all_commits)}")
```

#### 範例 4: 分析團隊成員在特定專案的貢獻
```python
from gitlab_collector import GitLabCollector

collector = GitLabCollector()
project_id = 123

# 取得所有使用者
users = collector.get_all_users()

# 分析每位使用者的貢獻
for user in users:
    stats = collector.get_user_statistics_in_project(
        project_id,
        user_email=user['email'],
        user_username=user['username']
    )
    
    commits = stats['commits']['total_commits']
    if commits > 0:
        print(f"{user['name']}: {commits} commits, "
              f"{stats['commits']['total_additions']} additions, "
              f"{stats['merge_requests']['total_mrs_created']} MRs")
```

## 輸出檔案

所有輸出檔案會儲存在 `./scripts/output/` 目錄下：

### 全體開發者分析輸出
- `all-user.commits.csv` - 所有 commit 記錄
- `all-user.code-changes.csv` - 程式碼異動詳情
- `all-user.merge-requests.csv` - Merge Request 資料
- `all-user.statistics.csv` - 開發者統計摘要

### 特定開發者分析輸出
- `{developer}.commits.csv` - 該開發者的所有 commit
- `{developer}.code-changes.csv` - 程式碼異動詳情
- `{developer}.merge-requests.csv` - 創建的 MR
- `{developer}.code-reviews.csv` - 參與審查的 MR
- `{developer}.statistics.csv` - 統計摘要
- `{developer}.report.txt` - 分析報告

## 分析指標

### 全體開發者分析可得指標
- ✅ 團隊整體活躍度
- ✅ 開發者貢獻度排名
- ✅ 程式碼品質趨勢
- ✅ Code Review 參與度比較

### 特定開發者分析可得指標

#### 程式碼品質指標
- ✅ Commit 頻率與規律性
- ✅ 程式碼變更量分布（平均、最大、最小）
- ✅ Commit 訊息品質
- ✅ 新增/刪除/重構程式碼比例
- ✅ 主要使用的程式語言/檔案類型
- ✅ Code Review 收到的意見數量

#### 技術水平指標
- ✅ 參與的專案數量與範圍
- ✅ 程式碼重構能力（重新命名、刪除舊程式碼）
- ✅ 團隊協作能力（創建 MR、參與 Review）
- ✅ 程式碼穩定性（MR 合併率）
- ✅ 響應速度（MR 更新頻率）
- ✅ 技術廣度（操作的檔案類型多樣性）

## 注意事項

1. **執行時間**：大型專案可能需要較長時間收集資料
2. **API 限制**：注意 GitLab API rate limit
3. **權限**：確保 Token 有足夠的權限存取目標專案
4. **資料隱私**：妥善保管包含開發者資訊的輸出檔案

## 疑難排解

### 連線錯誤
```
檢查 GITLAB_URL 和 GITLAB_TOKEN 是否正確
```

### 權限不足
```
確認 Token 權限包含 read_api 和 read_repository
```

### 資料過多
```
調整 START_DATE 和 END_DATE 縮小分析範圍
或設定 TARGET_GROUP_ID 只分析特定群組
```

## 使用範例

### 範例 1: 分析整個團隊
```bash
cd scripts
# 設定 config.py 中的日期範圍為 2024-01-01 到 2024-12-31
uv run gitlab_collector.py
```

### 範例 2: 分析特定開發者
```bash
cd scripts
# 使用 Email
uv run gitlab_developer_collector.py john.doe@example.com

# 使用 Username
uv run gitlab_developer_collector.py johndoe
```

### 範例 3: 批次分析多位開發者
```bash
cd scripts

# 創建開發者列表
cat > developers.txt << EOF
user1@example.com
user2@example.com
user3@example.com
EOF

# 批次執行
while read email; do
  echo "分析 $email ..."
  uv run gitlab_developer_collector.py "$email"
done < developers.txt
```

### 範例 4: 只分析特定專案組
```bash
# 編輯 config.py
# TARGET_GROUP_ID = 123  # 設定 Group ID

cd scripts
uv run gitlab_collector.py
```

## uv 常用指令

```bash
# 安裝相依套件
uv sync

# 新增套件
uv add <package-name>

# 移除套件
uv remove <package-name>

# 執行 Python 腳本
uv run <script.py>

# 更新所有套件
uv sync --upgrade
```

## 常見問題 FAQ

### Q1: 三種模式該如何選擇？
- **全體開發者分析**: 適合團隊管理、績效評估、尋找需要協助的成員
- **特定開發者分析**: 適合深入了解個人表現、一對一回饋、個人成長追蹤
- **程式化查詢 API**: 適合整合到自動化工具、客製化分析、定期報表產出

### Q2: 特定開發者分析找不到資料？
確認：
1. Email 或 Username 拼寫正確
2. 時間範圍涵蓋該開發者的活動期間
3. 有權限存取開發者參與的專案

### Q3: 可以同時分析多個開發者嗎？
可以，使用批次腳本（參考範例 3）或使用 Python API 寫迴圈處理。

### Q4: 輸出檔案太大怎麼辦？
- 縮小時間範圍（修改 config.py 的 START_DATE 和 END_DATE）
- 只分析特定專案（設定 TARGET_GROUP_ID 或 TARGET_PROJECT_IDS）
- 使用特定開發者分析模式而非全體分析

### Q5: 如何定期自動執行分析？
使用 cron (Linux/macOS) 或 Task Scheduler (Windows)：
```bash
# 每週一早上 8 點執行
0 8 * * 1 cd /path/to/scripts && uv run gitlab_collector.py
```

### Q6: Python API 如何處理大量資料？
```python
# 使用生成器或分批處理
from gitlab_collector import GitLabCollector

collector = GitLabCollector()
projects = collector.get_projects_list()

# 分批處理，避免一次載入過多資料
batch_size = 5
for i in range(0, len(projects), batch_size):
    batch_projects = projects[i:i+batch_size]
    for project in batch_projects:
        # 處理每個專案
        commits = collector.get_user_commits_in_project(
            project['id'], 
            user_email="user@example.com"
        )
        # 處理 commits...
```

### Q7: 如何匯出 JSON 格式而非 CSV？
```python
import json
from gitlab_collector import GitLabCollector

collector = GitLabCollector()
data = collector.get_user_statistics_in_project(123, user_email="user@example.com")

# 儲存為 JSON
with open('output.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)
```
