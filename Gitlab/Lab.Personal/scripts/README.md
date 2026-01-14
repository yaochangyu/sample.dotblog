# GitLab 開發者程式碼品質分析工具

> ⚠️ **重要更新**: 程式碼已重構！現提供統一的 CLI 介面 `gitlab_cli.py`，支援多種查詢模式。

## 🎯 目的

分析開發者的程式碼品質、技術水平，找出不足之處，協助團隊提升開發能力。

## ✨ 特色

- ✅ **統一 CLI** - 單一入口支援所有查詢模式
- ✅ **彈性查詢** - 支援全體/特定開發者、全部/特定專案
- ✅ **模組化架構** - 基於繼承的設計，減少重複程式碼
- ✅ **詳細資料** - 收集 commits、code changes、MRs、統計資訊
- ✅ **參數化配置** - 命令列參數完整支援

## 📋 功能說明

### 命令 1: user-info (查詢使用者資訊)

收集開發者的 commits、程式碼異動、merge requests 和統計資料。

#### 查詢所有開發者（指定時間範圍）
```bash
python gitlab_cli.py user-info --start-date 2024-01-01 --end-date 2024-12-31
```

**輸出檔案：**
- `all-user.commits.csv` - 所有 commit 記錄
- `all-user.merge-requests.csv` - Merge Request 資料
- `all-user.statistics.csv` - 開發者統計摘要

#### 查詢特定開發者（使用 Email）
```bash
python gitlab_cli.py user-info --developer-email user@example.com
```

**輸出檔案：**
- `{developer}.commits.csv` - 該開發者的所有 commit
- `{developer}.code-changes.csv` - 程式碼異動詳情
- `{developer}.merge-requests.csv` - 創建的 MR
- `{developer}.code-reviews.csv` - 參與審查的 MR
- `{developer}.statistics.csv` - 統計摘要
- `{developer}.report.txt` - 摘要報告

#### 查詢特定開發者（使用 Username）
```bash
python gitlab_cli.py user-info --developer-username johndoe
```

#### 查詢特定專案的使用者資訊
```bash
python gitlab_cli.py user-info --project-id 123,456
```

#### 組合查詢
```bash
# 特定開發者在特定專案的資料
python gitlab_cli.py user-info --developer-email user@example.com --project-id 123,456 --start-date 2024-01-01

# 特定群組的使用者資訊
python gitlab_cli.py user-info --group-id 789
```

### 命令 2: project-info (查詢專案資訊)

收集專案的基本資訊和統計資料。

#### 查詢所有專案
```bash
python gitlab_cli.py project-info
```

**輸出檔案：**
- `all-user.projects.csv` - 所有專案資訊

#### 查詢特定專案
```bash
python gitlab_cli.py project-info --project-id 123,456
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

### 完整命令列參數說明

#### user-info 命令
```bash
python gitlab_cli.py user-info [選項]

選項：
  --start-date TEXT           開始時間 (格式: YYYY-MM-DD)
  --end-date TEXT             結束時間 (格式: YYYY-MM-DD)
  --developer-email TEXT      特定開發者 email
  --developer-username TEXT   特定開發者 username
  --project-id TEXT           特定專案 ID (多個用逗號分隔)
  --group-id INTEGER          指定群組 ID
  -h, --help                  顯示說明訊息
```

#### project-info 命令
```bash
python gitlab_cli.py project-info [選項]

選項：
  --project-id TEXT           特定專案 ID (多個用逗號分隔)
  --group-id INTEGER          指定群組 ID
  -h, --help                  顯示說明訊息
```

### 使用範例

#### 範例 1: 分析團隊 2024 年的程式碼活動
```bash
python gitlab_cli.py user-info --start-date 2024-01-01 --end-date 2024-12-31
```

#### 範例 2: 檢視特定開發者的詳細報告
```bash
python gitlab_cli.py user-info --developer-email john.doe@example.com
```

#### 範例 3: 分析特定專案的貢獻者
```bash
python gitlab_cli.py user-info --project-id 123
```

#### 範例 4: 查看某開發者在特定專案的貢獻
```bash
python gitlab_cli.py user-info --developer-email user@example.com --project-id 123,456
```

#### 範例 5: 取得所有專案資訊
```bash
python gitlab_cli.py project-info
```

#### 範例 6: 背景執行（推薦用於大量資料）
```bash
nohup python gitlab_cli.py user-info > analyzer.log 2>&1 &

# 監控進度
tail -f analyzer.log
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

重構後的架構基於繼承，減少重複程式碼：

```
scripts/
├── gitlab_cli.py                    # 統一 CLI 入口（推薦使用）
├── base_gitlab_collector.py         # 基礎類別（共用邏輯）
├── gitlab_collector.py              # 全體開發者收集器（繼承基礎類別）
├── gitlab_developer_collector.py    # 特定開發者收集器（繼承基礎類別）
├── gitlab_client.py                 # GitLab API 客戶端
├── models.py                        # 資料模型
├── config.py                        # 配置檔
└── filters.py                       # 過濾策略（相容 gitlab_analyzer.py）

# 其他檔案
├── gitlab_analyzer.py               # 舊版統一介面（保留相容）
└── example_api_usage.py             # API 使用範例
```

**架構優點**：
- **繼承設計**: `BaseGitLabCollector` 提供共用功能
- **減少重複**: 共用的初始化、專案查詢、檔案儲存邏輯
- **彈性參數**: 支援自訂時間、專案、群組
- **向後相容**: 舊版檔案仍可使用

## 💡 使用範例

### 範例 1: 分析整個團隊特定時間範圍
```bash
python gitlab_cli.py user-info --start-date 2024-01-01 --end-date 2024-03-31
```

### 範例 2: 深入分析特定開發者
```bash
python gitlab_cli.py user-info --developer-email john.doe@example.com
```

### 範例 3: 檢視特定專案的所有貢獻者
```bash
python gitlab_cli.py user-info --project-id 123
```

### 範例 4: 批次分析多位開發者
```bash
# 創建開發者列表
cat > developers.txt << EOF
user1@example.com
user2@example.com
user3@example.com
EOF

# 批次執行
while read email; do
  echo "分析 $email ..."
  python gitlab_cli.py user-info --developer-email "$email"
done < developers.txt
```

### 範例 5: 程式化使用（進階）
```python
# custom_analysis.py
from gitlab_collector import GitLabCollector
from gitlab_developer_collector import GitLabDeveloperCollector

# 查詢特定專案的所有開發者資料
collector = GitLabCollector(
    start_date="2024-01-01",
    end_date="2024-12-31",
    project_ids=[123, 456]  # 只查詢這兩個專案
)

projects = collector.get_all_projects()
commits_df = collector.get_commits_data(projects)

print(f"收集了 {len(commits_df)} 筆 commits")

# 查詢特定開發者在特定專案的資料
dev_collector = GitLabDeveloperCollector(
    developer_email="user@example.com",
    start_date="2024-01-01",
    end_date="2024-12-31",
    project_ids=[123]  # 只查詢專案 123
)

projects = dev_collector.get_all_projects()
commits_df = dev_collector.get_commits_data(projects)
changes_df = dev_collector.get_code_changes_data(projects)
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

### Q1: 舊版檔案還能用嗎？
可以，`gitlab_collector.py`、`gitlab_developer_collector.py` 和 `gitlab_analyzer.py` 都保留向後相容。但建議使用新版 `gitlab_cli.py`，介面更統一且參數更彈性。

### Q2: 如何選擇使用方式？
- **gitlab_cli.py** (推薦): 統一 CLI 介面，支援所有查詢模式
- **gitlab_analyzer.py**: 舊版統一介面，遵循 SOLID 原則
- **直接 import 模組**: 需要客製化或整合到其他程式

### Q3: user-info 和 project-info 有什麼差別？
- **user-info**: 收集開發者活動資料（commits、MRs、統計）
- **project-info**: 只收集專案基本資訊（名稱、描述、成員）

### Q4: 可以同時指定多個專案嗎？
可以，使用逗號分隔：`--project-id 123,456,789`

### Q5: 輸出檔案太大怎麼辦？
- 縮小時間範圍：`--start-date 2024-01-01 --end-date 2024-01-31`
- 只分析特定專案：`--project-id 123`
- 只分析特定開發者：`--developer-email user@example.com`

### Q6: 如何定期自動執行分析？
使用 cron (Linux/macOS) 或 Task Scheduler (Windows)：
```bash
# 每週一早上 8 點執行
0 8 * * 1 cd /path/to/scripts && python gitlab_cli.py user-info
```

### Q7: 新版相比舊版有什麼改進？
- ✅ 統一 CLI 入口，不需記憶多個檔案
- ✅ 完整的命令列參數支援
- ✅ 基於繼承的架構，減少重複程式碼
- ✅ 彈性的專案/群組/時間範圍設定
- ✅ 更清晰的輸出訊息

## 📚 相關檔案

- `gitlab_cli.py` - 統一 CLI 入口（推薦使用）
- `base_gitlab_collector.py` - 基礎類別
- `gitlab_collector.py` - 全體開發者收集器
- `gitlab_developer_collector.py` - 特定開發者收集器
- `gitlab_analyzer.py` - 舊版統一介面（保留相容）
- `example_api_usage.py` - API 使用範例
- `config.py` - 配置檔範本

## 📄 授權

本專案僅供學習與內部使用，請勿用於商業用途。
