# GitLab 開發者程式碼品質分析工具

## 功能說明

這個工具提供兩種分析模式：

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

### Q1: 兩種模式該如何選擇？
- **全體開發者分析**: 適合團隊管理、績效評估、尋找需要協助的成員
- **特定開發者分析**: 適合深入了解個人表現、一對一回饋、個人成長追蹤

### Q2: 特定開發者分析找不到資料？
確認：
1. Email 或 Username 拼寫正確
2. 時間範圍涵蓋該開發者的活動期間
3. 有權限存取開發者參與的專案

### Q3: 可以同時分析多個開發者嗎？
可以，使用批次腳本（參考範例 3）或寫個簡單的 shell 迴圈。

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
