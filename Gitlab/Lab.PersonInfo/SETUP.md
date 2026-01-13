# 🔧 環境設定指南

## 1. 安裝 uv 套件管理工具

### macOS / Linux

```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### Windows

```powershell
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"
```

### 或使用 pip

```bash
pip install uv
```

---

## 2. 安裝專案依賴

```bash
# 切換到專案目錄
cd Lab.PersonInfo

# 安裝所有依賴
uv sync

# 或只安裝生產環境依賴
uv sync --no-dev
```

---

## 3. 設定 GitLab Personal Access Token

### 步驟 1：建立 Token

1. 前往 GitLab → **Settings** → **Access Tokens**
   - GitLab.com: https://gitlab.com/-/profile/personal_access_tokens
   - 私有 GitLab: `https://your-gitlab-instance.com/-/profile/personal_access_tokens`

2. 填寫 Token 資訊：
   - **Token name**: `developer-analyzer`（或任意名稱）
   - **Expiration date**: 建議設定 90 天後（或根據需求）
   - **Select scopes**（權限）：勾選以下三項 ✅
     - `read_api` - 讀取 API 資源
     - `read_repository` - 讀取 Repository
     - `read_user` - 讀取用戶資訊

3. 點擊 **Create personal access token**

4. **重要**：複製產生的 Token（只會顯示一次！）
   ```
   範例格式：glpat-xxxxxxxxxxxxxxxxxxxx
   ```

---

### 步驟 2：設定環境變數

#### 方法 A：使用 .env 檔案（推薦）

```bash
# 複製範本
cp .env.example .env

# 編輯 .env 檔案
nano .env  # 或使用任何文字編輯器
```

在 `.env` 檔案中填入 Token：

```bash
# GitLab 連線設定
GITLAB_URL=https://gitlab.com
GITLAB_TOKEN=glpat-your_actual_token_here  # 替換成你的 Token
```

**私有 GitLab 實例**：
```bash
GITLAB_URL=https://gitlab.your-company.com
GITLAB_TOKEN=glpat-your_actual_token_here
```

---

#### 方法 B：直接設定環境變數（臨時）

**Linux / macOS**：
```bash
export GITLAB_URL="https://gitlab.com"
export GITLAB_TOKEN="glpat-your_actual_token_here"
```

**Windows PowerShell**：
```powershell
$env:GITLAB_URL="https://gitlab.com"
$env:GITLAB_TOKEN="glpat-your_actual_token_here"
```

**Windows CMD**：
```cmd
set GITLAB_URL=https://gitlab.com
set GITLAB_TOKEN=glpat-your_actual_token_here
```

---

## 4. 測試連線

執行測試腳本驗證設定是否正確：

```bash
# 使用 uv run 執行
uv run python scripts/test_connection.py

# 或直接執行 gitlab_config.py
uv run python scripts/config/gitlab_config.py
```

**預期輸出**：

```
🔗 連線到 GitLab: https://gitlab.com
✅ 認證成功！使用者: your_username
✅ 連線測試成功！
   使用者: your_username
   Email: your_email@example.com
   ID: 123456

============================================================
測試可訪問的專案（前 5 個）
============================================================

找到 5 個專案：

  📦 Project Name 1
     ID: 12345
     Path: group/project-name-1
     URL: https://gitlab.com/group/project-name-1

  ...
```

---

## 5. 常見問題排解

### ❌ 錯誤：未設定 GITLAB_TOKEN 環境變數

**原因**：沒有建立 `.env` 檔案或 Token 沒有設定

**解決**：
1. 確認已複製 `.env.example` 為 `.env`
2. 確認 `.env` 檔案中的 `GITLAB_TOKEN` 有填入實際的 Token

---

### ❌ GitLab Token 認證失敗

**原因**：Token 無效、權限不足或已過期

**解決**：
1. 檢查 Token 是否正確（包含 `glpat-` 前綴）
2. 確認 Token 權限包含：
   - `read_api`
   - `read_repository`
   - `read_user`
3. 檢查 Token 是否已過期（前往 GitLab Access Tokens 頁面查看）
4. 如果過期，重新建立一個新的 Token

---

### ❌ 連線逾時或網路錯誤

**原因**：網路問題或 GitLab URL 錯誤

**解決**：
1. 確認 `GITLAB_URL` 設定正確
   - GitLab.com: `https://gitlab.com`
   - 私有實例: `https://gitlab.your-company.com`
2. 確認網路可以訪問 GitLab
3. 如果使用私有 GitLab，確認 VPN 已連線

---

### ❌ ModuleNotFoundError: No module named 'gitlab'

**原因**：依賴套件未安裝

**解決**：
```bash
# 重新安裝依賴
uv sync
```

---

### ❌ Rate Limiting (429 Too Many Requests)

**原因**：API 請求過於頻繁

**解決**：
在 `.env` 檔案中調整請求間隔：
```bash
API_REQUEST_DELAY=0.5  # 增加到 0.5 秒
```

---

## 6. 安全注意事項

### ⚠️ 絕對不要將 Token 提交到 Git

- `.env` 檔案已加入 `.gitignore`，不會被提交
- 不要將 Token 寫在程式碼中
- 不要將 Token 分享給他人
- 定期更換 Token（建議 3-6 個月）

### 🔒 Token 權限最小化原則

只勾選必要的權限：
- ✅ `read_api` - 必要
- ✅ `read_repository` - 必要
- ✅ `read_user` - 必要
- ❌ `write_*` - 不需要（本系統只讀取數據）
- ❌ `admin_*` - 不需要

---

## 7. 驗證設定完成

執行以下命令確認一切正常：

```bash
# 1. 確認 uv 已安裝
uv --version

# 2. 確認依賴已安裝
uv run python -c "import gitlab; print('✅ python-gitlab 已安裝')"

# 3. 測試 GitLab 連線
uv run python scripts/test_connection.py

# 4. 查看可訪問的專案
uv run python scripts/config/gitlab_config.py
```

如果以上步驟都成功，恭喜您已完成環境設定！🎉

---

## 8. 下一步

環境設定完成後，您可以：

```bash
# 分析單一開發者
uv run python scripts/main.py analyze --user "開發者名稱" --from "2024-01-01" --to "2024-12-31"

# 批次分析所有開發者
uv run python scripts/main.py analyze-all --from "2024-01-01" --to "2024-12-31"
```

詳細使用方式請參考 `README.md`。

---

**文件版本**：v1.0
**最後更新**：2026-01-13
