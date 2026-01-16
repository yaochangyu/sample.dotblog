# GitLab CLI 2 - 開發者程式碼品質與技術水平分析工具

## 📋 功能說明

這個工具專門用於分析 GitLab 開發者的程式碼品質和技術水平，提供以下詳細資訊：

### 收集的資料類型

1. **用戶個人資料** (`user_profile`)
   - 基本資訊（ID, 用戶名, 姓名, Email）
   - 帳號狀態、建立時間、最後活動時間
   - 職位、組織、位置等

2. **Commit 記錄** (`commits`)
   - 所有 commit 的詳細資訊
   - 包含標題、訊息、作者、日期
   - 統計資料（新增行數、刪除行數、總變更行數）

3. **程式碼變更** (`code_changes`)
   - 每個 commit 變更了哪些檔案
   - 檔案路徑、diff 內容
   - 檔案狀態（新增、修改、刪除、重新命名）

4. **Merge Requests** (`merge_requests`)
   - MR 標題、描述、狀態
   - 建立時間、更新時間、合併時間
   - 評論數、讚數

5. **Code Review** (`code_reviews`)
   - MR 討論串和評論
   - 評論者、評論內容、評論時間
   - 是否已解決

6. **貢獻者統計** (`contributors`)
   - 每個專案的貢獻統計
   - 總 commits 數、總新增行數、總刪除行數

7. **授權資訊** (`permissions`)
   - 在各專案的權限等級
   - Guest/Reporter/Developer/Maintainer/Owner

8. **統計摘要** (`statistics`)
   - 總專案數、總 commits 數
   - 總新增/刪除行數
   - MR 統計、Code Review 統計

## 🚀 快速開始

### 安裝依賴

```bash
# 使用 uv（推薦）
uv sync

# 或使用 pip
pip install -r requirements.txt
```

### 設定檔

編輯 `config.py` 設定以下參數：

```python
# GitLab 連線設定
GITLAB_URL = "https://your-gitlab.com/"
GITLAB_TOKEN = "your-access-token"

# 預設分析時間範圍
START_DATE = "2024-01-01"
END_DATE = "2024-12-31"

# 可選：指定要分析的 Group 或 Project
TARGET_GROUP_ID = None      # 例如：123
TARGET_PROJECT_IDS = []     # 例如：[456, 789]
```

## 📖 使用方式

### 基本語法

```bash
uv run python gl-cli-2.py user-details [OPTIONS]
```

### 參數說明

| 參數 | 必填 | 說明 | 範例 |
|------|------|------|------|
| `--username` | 否 | 用戶名稱（可指定多個，用空格分隔）<br>不指定則分析所有用戶 | `--username alice bob` |
| `--project-name` | 否 | 專案名稱（可指定多個，用空格分隔）<br>不指定則分析所有專案 | `--project-name web-api mobile-app` |
| `--start-date` | 否 | 開始日期 (YYYY-MM-DD)<br>預設使用 config.py 的 START_DATE | `--start-date 2024-01-01` |
| `--end-date` | 否 | 結束日期 (YYYY-MM-DD)<br>預設使用 config.py 的 END_DATE | `--end-date 2024-12-31` |
| `--output` | 否 | 輸出目錄<br>預設為 `./output-2` | `--output ./reports` |

## 📚 使用範例

### 1. 分析單一用戶

```bash
# 分析特定用戶的所有資料
uv run python gl-cli-2.py user-details --username "G2023018"
```

**輸出檔案：**
- `G2023018-user_profile.csv`
- `G2023018-user_commits.csv`
- `G2023018-user_code_changes.csv`
- `G2023018-user_merge_requests.csv`
- `G2023018-user_code_reviews.csv`
- `G2023018-user_contributors.csv`
- `G2023018-user_permissions.csv`
- `G2023018-user_statistics.csv`

### 2. 分析多個用戶

```bash
# 同時分析多個用戶
uv run python gl-cli-2.py user-details --username "G2023018" "G2023017" "alice"
```

### 3. 分析特定專案的用戶

```bash
# 分析單一用戶在特定專案的資料
uv run python gl-cli-2.py user-details \
  --username "G2023018" \
  --project-name "web-components-vue3"

# 分析多個用戶在多個專案的資料
uv run python gl-cli-2.py user-details \
  --username "G2023018" "G2023017" \
  --project-name "web-components-vue3" "web-api"
```

### 4. 分析所有用戶

```bash
# 分析所有專案的所有用戶
uv run python gl-cli-2.py user-details

# 分析特定專案的所有用戶
uv run python gl-cli-2.py user-details --project-name "web-api"
```

**輸出檔案：**
- `all-users-commits.csv`
- `all-users-merge_requests.csv`
- 等等...

### 5. 指定日期範圍

```bash
# 分析 2024 年的資料
uv run python gl-cli-2.py user-details \
  --username "G2023018" \
  --start-date 2024-01-01 \
  --end-date 2024-12-31

# 分析最近三個月
uv run python gl-cli-2.py user-details \
  --username "G2023018" \
  --start-date 2024-10-01 \
  --end-date 2024-12-31
```

### 6. 自訂輸出目錄

```bash
uv run python gl-cli-2.py user-details \
  --username "G2023018" \
  --output ./reports/2024
```

## 📊 輸出格式

### CSV 格式
- 所有資料以 CSV 格式輸出
- 使用 UTF-8 BOM 編碼，中文顯示正常
- 適合用 Excel、Google Sheets 開啟
- 方便進行數據分析和製作圖表

### 檔名規則

**單一用戶：**
```
{username}-user_{data_type}.csv
```
範例：
- `G2023018-user_commits.csv`
- `alice-user_merge_requests.csv`

**所有用戶：**
```
all-users-{data_type}.csv
```
範例：
- `all-users-commits.csv`
- `all-users-statistics.csv`

## 🎯 使用場景

### 1. 年度績效評估
```bash
# 取得 2024 年度特定開發者的資料
uv run python gl-cli-2.py user-details \
  --username "alice" "bob" "charlie" \
  --start-date 2024-01-01 \
  --end-date 2024-12-31
```

### 2. 新人培訓評估
```bash
# 追蹤新進員工的成長
uv run python gl-cli-2.py user-details \
  --username "new_developer" \
  --start-date 2024-06-01
```

### 3. 專案健康度檢查
```bash
# 檢查某個專案的開發狀況
uv run python gl-cli-2.py user-details \
  --project-name "critical-project"
```

### 4. Code Review 品質分析
```bash
# 分析團隊的 Code Review 參與度
uv run python gl-cli-2.py user-details \
  --start-date 2024-01-01
# 然後查看 *-user_code_reviews.csv
```

## 📈 分析指標建議

### Commit 品質指標
- **提交頻率**：`commits.csv` 中的 `committed_date` 分布
- **程式碼規模**：`commits.csv` 中的 `additions/deletions` 比例
- **Commit 訊息品質**：`commits.csv` 中的 `title` 和 `message` 內容

### Code Review 指標
- **參與度**：`code_reviews.csv` 中的評論數量
- **回應速度**：`code_reviews.csv` 中的 `created_at` 與 MR `created_at` 的時間差
- **問題解決率**：`code_reviews.csv` 中的 `resolved` 比例

### 貢獻度指標
- **程式碼貢獻量**：`contributors.csv` 中的 `commits/additions/deletions`
- **專案參與度**：`statistics.csv` 中的 `total_projects`
- **MR 合併率**：`merge_requests.csv` 中 `merged` 狀態的比例

## 💡 進階技巧

### 1. 批次分析多個用戶

建立一個腳本檔案 `analyze_team.sh`：

```bash
#!/bin/bash
uv run python gl-cli-2.py user-details \
  --username alice bob charlie david \
  --start-date 2024-01-01 \
  --output ./team-reports
```

### 2. 排程執行

使用 cron (Linux/Mac) 或 Task Scheduler (Windows) 定期執行：

```bash
# 每週一早上 8 點執行
0 8 * * 1 cd /path/to/scripts && uv run python gl-cli-2.py user-details
```

### 3. 與 Excel 整合

1. 開啟產生的 CSV 檔案
2. 使用樞紐分析表分析資料
3. 建立圖表視覺化

### 4. 資料合併分析

使用 pandas 合併多個 CSV：

```python
import pandas as pd

# 合併所有 commits
commits = pd.read_csv('alice-user_commits.csv')
stats = commits.groupby('project_name').agg({
    'commit_id': 'count',
    'additions': 'sum',
    'deletions': 'sum'
})
print(stats)
```

## 🔑 權限需求

GitLab Access Token 需要以下權限：
- `read_api` - 讀取 API 資料
- `read_repository` - 讀取程式碼庫

建議使用 **Reporter (20)** 以上的權限等級。

## 🐛 常見問題

### Q1: 為什麼某些用戶沒有資料？

A: 可能原因：
- 用戶在指定時間範圍內沒有活動
- 用戶在指定專案中沒有貢獻
- 用戶名稱拼寫錯誤

### Q2: CSV 檔案中文亂碼？

A: 使用 Excel 開啟時，選擇「UTF-8 with BOM」編碼。本工具已自動使用此編碼。

### Q3: 執行速度很慢？

A: 
- 減少分析的專案數量（使用 `--project-name` 指定）
- 縮小日期範圍
- GitLab API 有速率限制，耐心等待

### Q4: 記憶體不足？

A: 程式碼已針對大量資料做了優化（限制 commits diff 數量），如仍不足可修改程式碼中的限制。

### Q5: 如何查看幫助？

```bash
# 主幫助
uv run python gl-cli-2.py --help

# user-details 子命令幫助
uv run python gl-cli-2.py user-details --help
```

## 📝 技術細節

### 資料收集流程

1. **取得目標專案**：根據 `--project-name` 或 `config.py` 設定
2. **取得目標用戶**：
   - 有指定 `--username`：從 GitLab 用戶列表查詢
   - 未指定：從專案貢獻者中收集
3. **分析每個用戶**：
   - 遍歷每個專案
   - 收集各類資料（commits, MRs, reviews 等）
   - 計算統計資訊
4. **儲存結果**：輸出 CSV 檔案

### 效能優化

- Code Changes：限制前 50 個 commits 的 diff
- Code Reviews：限制前 20 個 MR 的討論串
- Diff 內容：限制每個檔案 500 字元

## 🔄 與舊版本的差異

### gl-cli-2.py vs gl-cli.py

| 特性 | gl-cli-2.py | gl-cli.py |
|------|-------------|-----------|
| 命令格式 | `user-details` 子命令 | 直接參數 |
| 用戶參數 | `--username alice bob` | `--username alice --username bob` |
| 輸出格式 | 僅 CSV | CSV + Markdown |
| 預設輸出目錄 | `./output-2` | `./output` |
| 方法設計 | 統一 `analyze_users()` | 分離 `get_user_detail()` 和 `analyze_users()` |

## 📚 參考資料

- [GitLab API 文件](https://docs.gitlab.com/ee/api/)
- [python-gitlab 函式庫](https://python-gitlab.readthedocs.io/)
- [Pandas 文件](https://pandas.pydata.org/docs/)

## 📧 支援

如有問題或建議，請聯繫開發團隊或提交 Issue。

---

**開發者**: GitLab Analysis Team  
**最後更新**: 2026-01-16  
**版本**: 2.0
