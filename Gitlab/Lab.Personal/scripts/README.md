# GitLab 開發者程式碼品質分析工具

## 功能說明

這個工具用於收集和分析 GitLab 上開發者的程式碼品質與技術水平，包括：

### 收集的資料

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

### 方式 1：使用 uv run

```bash
cd scripts
uv run gitlab_collector.py
```

### 方式 2：使用 uvx (一次性執行)

```bash
cd scripts
uvx --from python-gitlab --with pandas --with tqdm python gitlab_collector.py
```

### 方式 3：進入虛擬環境

```bash
cd scripts
uv sync
source .venv/bin/activate  # Linux/macOS
# 或
.venv\Scripts\activate  # Windows

python gitlab_collector.py
```

## 輸出檔案

所有輸出檔案會儲存在 `./scripts/output/` 目錄下：

- `all-user.commits.csv` - 所有 commit 記錄
- `all-user.code-changes.csv` - 程式碼異動詳情
- `all-user.merge-requests.csv` - Merge Request 資料
- `all-user.statistics.csv` - 開發者統計摘要

## 分析指標

透過收集的資料，可以分析以下指標：

### 程式碼品質指標
- ✅ Commit 頻率與規律性
- ✅ 程式碼變更量（避免過大的 commit）
- ✅ Commit 訊息品質
- ✅ Code Review 參與度
- ✅ MR 的討論與互動

### 技術水平指標
- ✅ 參與的專案數量
- ✅ 程式碼重構能力（重新命名、刪除舊程式碼）
- ✅ 團隊協作能力（Review 他人程式碼）
- ✅ 程式碼穩定性（MR 合併率）

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
