# GitLab 開發者程式碼品質分析工具

> ⚠️ **重要更新**: 程式碼已重構！新版本使用 `gitlab_analyzer.py`，遵循 SOLID 原則，減少 70% 重複程式碼。

## 🎯 目的

分析開發者的程式碼品質、技術水平，找出不足之處，協助團隊提升開發能力。

## ✨ 特色

- ✅ **統一介面** - 一個檔案支援所有分析模式
- ✅ **遵循 SOLID** - 使用策略模式，容易擴展
- ✅ **程式化 API** - 可整合到其他工具
- ✅ **詳細資料** - 收集 commits、code changes、MRs、統計資訊
- ✅ **彈性過濾** - 支援全體開發者或特定開發者分析

## 📋 功能說明

### 模式 1: 全體開發者分析

收集所有開發者的資料，用於團隊整體分析和比較。

```bash
# 分析所有開發者
uv run gitlab_analyzer.py
```

**輸出檔案：**
- `all-user.commits.csv` - 所有 commit 記錄（包含程式碼變更量、異動檔案）
- `all-user.code-changes.csv` - 程式碼異動詳情（檔案路徑、新增/刪除/重新命名）
- `all-user.merge-requests.csv` - Merge Request 資料（狀態、審查者、評論）
- `all-user.statistics.csv` - 開發者統計摘要（總 commits、程式碼量、參與專案數）

### 模式 2: 特定開發者深度分析

針對單一開發者進行詳細分析，提供更多細節資訊。

```bash
# 使用 Email 分析
uv run gitlab_analyzer.py user@example.com

# 使用 Username 分析
uv run gitlab_analyzer.py johndoe
```

**輸出檔案：**
- `{developer}.commits.csv` - 該開發者的所有 commit
- `{developer}.code-changes.csv` - 程式碼異動詳情（包含 diff 內容）
- `{developer}.merge-requests.csv` - 創建的 MR 完整資訊
- `{developer}.code-reviews.csv` - 參與審查的 MR 列表
- `{developer}.statistics.csv` - 統計摘要（檔案類型分析、MR 合併率）

### 模式 3: 程式化查詢 API

整合到其他 Python 程式，進行客製化分析。

```python
from gitlab_analyzer import GitLabCollector
from filters import SpecificDeveloperFilter

# 分析特定開發者
filter_strategy = SpecificDeveloperFilter(email="user@example.com")
collector = GitLabCollector(filter_strategy=filter_strategy)

# 取得所有專案
projects = collector.get_projects_list()

# 取得所有使用者
users = collector.get_all_users()

# 查詢特定使用者在特定專案的資料
commits = collector.get_user_commits_in_project(
    project_id=123,
    user_email="user@example.com"
)

statistics = collector.get_user_statistics_in_project(
    project_id=123,
    user_email="user@example.com",
    user_username="johndoe"
)
```

## 🚀 安裝步驟

### 1. 安裝 uv

```bash
# Windows (PowerShell)
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# macOS/Linux
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### 2. 安裝相依套件

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

編輯 `config.py`：

```python
GITLAB_URL = "https://gitlab.com"  # 或您的 GitLab 伺服器網址
GITLAB_TOKEN = "YOUR_TOKEN_HERE"   # 貼上您的 Access Token

START_DATE = "2024-01-01"  # 分析起始日期
END_DATE = "2024-12-31"    # 分析結束日期

# 可選：只分析特定群組或專案
TARGET_GROUP_ID = None  # 例如: 123
TARGET_PROJECT_IDS = []  # 例如: [456, 789]
```

## 📖 使用方式

### A. 全體開發者分析

```bash
cd scripts
uv run gitlab_analyzer.py
```

### B. 特定開發者分析

```bash
# 使用 Email
uv run gitlab_analyzer.py user@example.com

# 使用 Username
uv run gitlab_analyzer.py johndoe
```

### C. 背景執行（推薦用於大量資料）

```bash
nohup uv run gitlab_analyzer.py > analyzer.log 2>&1 &

# 監控進度
tail -f analyzer.log
```

### D. 程式化查詢

創建自己的分析腳本：

```python
# my_analysis.py
from gitlab_analyzer import GitLabCollector
from filters import AllDevelopersFilter, SpecificDeveloperFilter

# 範例 1: 分析所有開發者
collector = GitLabCollector()
projects = collector.get_all_projects()
commits_df = collector.collect_commits(projects[:5])  # 只分析前 5 個專案

# 範例 2: 分析特定開發者
filter_strategy = SpecificDeveloperFilter(email="user@example.com")
collector = GitLabCollector(filter_strategy=filter_strategy)
projects = collector.get_all_projects()
commits_df = collector.collect_commits(projects)

# 範例 3: 跨專案統計
users = collector.get_all_users()
for user in users[:10]:  # 只分析前 10 位使用者
    stats = collector.get_user_statistics_in_project(
        project_id=123,
        user_email=user['email']
    )
    print(f"{user['name']}: {stats['commits']['total_commits']} commits")
```

## 📊 收集的資料

### 1. 專案資訊
- ID、名稱、描述、路徑
- 建立時間、最後活動時間
- Stars、Forks、Issues 數量

### 2. 使用者資訊
- ID、Username、名稱、Email
- 帳號狀態、建立時間
- 最後登入時間、活動時間

### 3. Commit 記錄
- Commit ID、作者、提交時間
- 程式碼新增/刪除行數
- Commit 訊息
- 異動的檔案列表

### 4. 程式碼異動
- 檔案路徑、檔案類型
- 新增/刪除/重新命名的檔案
- 新增/刪除的行數
- Diff 內容（前 5000 字元）

### 5. Code Review (MR)
- MR 標題、描述、狀態
- 作者、審查者、指派者
- 建立/更新/合併時間
- 評論數、討論內容
- 變更的檔案、Commits
- 標籤、里程碑

### 6. 統計資訊
- **Commits**: 總數、新增/刪除行數、平均變更量
- **Files**: 異動檔案數、檔案類型分布、最常修改的檔案
- **MR**: 創建/合併數、合併率、評論數
- **Productivity**: 每日 commit 數、每日變更量、活躍天數

## 🔍 分析指標

### 程式碼品質指標
- ✅ Commit 頻率與規律性
- ✅ 程式碼變更量分布（平均、最大、最小）
- ✅ Commit 訊息品質
- ✅ 新增/刪除/重構程式碼比例
- ✅ 主要使用的程式語言/檔案類型
- ✅ Code Review 收到的意見數量

### 技術水平指標
- ✅ 參與的專案數量與範圍
- ✅ 程式碼重構能力（重新命名、刪除舊程式碼）
- ✅ 團隊協作能力（創建 MR、參與 Review）
- ✅ 程式碼穩定性（MR 合併率）
- ✅ 響應速度（MR 更新頻率）
- ✅ 技術廣度（操作的檔案類型多樣性）

## 🏗️ 架構說明

重構後的架構遵循 SOLID 原則：

```
scripts/
├── gitlab_analyzer.py   # 主要收集器（統一入口）
├── filters.py           # 過濾策略（AllDevelopersFilter, SpecificDeveloperFilter）
├── models.py            # 資料模型（Project, User, Commit, MR 等）
├── config.py            # 配置檔
└── example_api_usage.py # API 使用範例

# 舊版檔案（保留向後相容）
├── gitlab_collector.py          # 舊版：全體開發者分析
└── gitlab_developer_collector.py # 舊版：特定開發者分析
```

**設計原則**：
- **Single Responsibility**: 各模組職責單一（收集、過濾、模型分離）
- **Open/Closed**: 透過策略模式擴展（新增過濾器不需修改主程式）
- **Liskov Substitution**: FilterStrategy 可替換
- **Interface Segregation**: 清晰的方法介面
- **Dependency Inversion**: 依賴抽象的 FilterStrategy

## 💡 使用範例

### 範例 1: 分析整個團隊

```bash
cd scripts
uv run gitlab_analyzer.py
```

### 範例 2: 分析特定開發者

```bash
# 使用 Email
uv run gitlab_analyzer.py john.doe@example.com

# 使用 Username
uv run gitlab_analyzer.py johndoe
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
  uv run gitlab_analyzer.py "$email"
done < developers.txt
```

### 範例 4: 客製化分析腳本

```python
# custom_analysis.py
from gitlab_analyzer import GitLabCollector
from filters import SpecificDeveloperFilter
import json

# 分析特定開發者在所有專案的貢獻
collector = GitLabCollector()
projects = collector.get_projects_list()

developer_email = "user@example.com"
all_commits = []

for project in projects:
    commits = collector.get_user_commits_in_project(
        project['id'],
        user_email=developer_email
    )
    all_commits.extend(commits)

# 輸出為 JSON
with open('developer_report.json', 'w') as f:
    json.dump({
        'email': developer_email,
        'total_commits': len(all_commits),
        'projects_contributed': len(projects),
        'commits': all_commits
    }, f, indent=2)
```

## ⚠️ 注意事項

1. **執行時間**: 大型專案可能需要較長時間收集資料
2. **API 限制**: 注意 GitLab API rate limit
3. **權限**: 確保 Token 有足夠的權限存取目標專案
4. **資料隱私**: 妥善保管包含開發者資訊的輸出檔案
5. **專案數量**: 預設只處理前 5 個專案，可修改 `gitlab_analyzer.py` 的 `main()` 函數

## 🔧 疑難排解

### 連線錯誤
```
檢查 GITLAB_URL 和 GITLAB_TOKEN 是否正確
```

### 權限不足
```
確認 Token 權限包含 read_api 和 read_repository
```

### 找不到開發者資料
```
確認 Email 或 Username 拼寫正確
確認時間範圍涵蓋該開發者的活動期間
```

### ImportError
```bash
# 確保在正確的目錄
cd scripts

# 重新安裝相依套件
uv sync
```

## ❓ 常見問題

### Q1: 舊版 `gitlab_collector.py` 和 `gitlab_developer_collector.py` 還能用嗎？
可以，舊版檔案保留向後相容。但建議使用新版 `gitlab_analyzer.py`，功能更完整且效能更好。

### Q2: 如何選擇分析模式？
- **全體開發者分析**: 團隊管理、績效評估、尋找需要協助的成員
- **特定開發者分析**: 深入了解個人表現、一對一回饋、個人成長追蹤
- **程式化 API**: 整合到自動化工具、客製化分析、定期報表產出

### Q3: 可以分析多個開發者嗎？
可以，使用批次腳本（參考範例 3）或使用 Python API 寫迴圈處理。

### Q4: 輸出檔案太大怎麼辦？
- 縮小時間範圍（修改 `config.py` 的 START_DATE 和 END_DATE）
- 只分析特定專案（設定 TARGET_GROUP_ID 或 TARGET_PROJECT_IDS）
- 使用特定開發者分析模式

### Q5: 如何定期自動執行分析？
使用 cron (Linux/macOS) 或 Task Scheduler (Windows)：
```bash
# 每週一早上 8 點執行
0 8 * * 1 cd /path/to/scripts && uv run gitlab_analyzer.py
```

### Q6: 新版相比舊版有什麼改進？
- ✅ 減少 70% 重複程式碼
- ✅ 遵循 SOLID 原則，容易擴展
- ✅ 統一介面，一個檔案支援所有模式
- ✅ 更好的錯誤處理
- ✅ 更詳細的資料收集

## 📚 相關檔案

- `ANALYSIS.md` - 程式碼分析報告（重構前後比較）
- `example_api_usage.py` - API 使用範例（8 個實用範例）
- `config.py` - 配置檔範本

## 📄 授權

本專案僅供學習與內部使用，請勿用於商業用途。
