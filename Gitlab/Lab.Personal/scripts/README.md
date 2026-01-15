# GitLab 開發者程式碼品質分析工具

[![Python](https://img.shields.io/badge/python-3.10+-blue.svg)](https://www.python.org/downloads/)
[![UV](https://img.shields.io/badge/uv-latest-green.svg)](https://github.com/astral-sh/uv)
[![GitLab API](https://img.shields.io/badge/GitLab%20API-v4-orange.svg)](https://docs.gitlab.com/ee/api/)

> 🎯 **資深 GitLab 專家級 CLI 工具** - 深度分析開發者程式碼品質與技術水平

---

## 📚 文件快速導航

| 文件 | 說明 | 適合對象 |
|------|------|----------|
| **[🚀 QUICKSTART.md](./QUICKSTART.md)** | **5 分鐘快速開始** | ⭐ 新手必讀 |
| [📖 GL-CLI-README.md](./GL-CLI-README.md) | 完整詳細文件 | 深入使用者 |
| [🏗️ PROJECT-SUMMARY.md](./PROJECT-SUMMARY.md) | 技術架構摘要 | 技術人員 |
| [📁 FILES-MANIFEST.md](./FILES-MANIFEST.md) | 檔案清單 | 開發者 |

**👉 第一次使用？** 請直接閱讀 [QUICKSTART.md](./QUICKSTART.md)

---

## ⚡ 超快速開始（3 步驟）

```bash
# 1. 安裝 UV
curl -LsSf https://astral.sh/uv/install.sh | sh  # macOS/Linux

# 2. 安裝相依套件
cd scripts && uv sync

# 3. 設定 Token（編輯 config.py）
GITLAB_URL = "https://gitlab.com"
GITLAB_TOKEN = "your_token_here"

# 開始使用
uv run python gl-cli.py project-stats
```

詳細步驟請參考 [QUICKSTART.md](./QUICKSTART.md)

---

## 🎯 四大核心功能

### 1️⃣ 專案資訊查詢 (`project-stats`)
查詢專案基本資料、活動狀態、統計數據、**授權統計**

```bash
# 所有專案（包含授權統計）
uv run python gl-cli.py project-stats

# 特定專案（包含授權統計）
uv run python gl-cli.py project-stats --project-name "web-app"
```

**輸出檔案：**
- `all-project-stats.{csv,md}` - 專案資料 + 授權統計
- `all-project-stats-permissions.{csv,md}` - 授權詳細資料

**新增授權統計欄位（8 個）：**
- `total_members` - 總成員數
- `user_members` / `group_members` - 使用者/群組成員數
- `owners` / `maintainers` / `developers` / `reporters` / `guests` - 各權限等級人數

**實際測試：** 已驗證，成功獲取 378 個專案 + 授權資訊

---

### 2️⃣ 群組資訊查詢 (`group-stats`) 🆕
查詢群組完整資訊、子群組、專案、**授權統計**

```bash
# 所有群組
uv run python gl-cli.py group-stats

# 特定群組
uv run python gl-cli.py group-stats --group-name "my-group"
```

**輸出檔案：**
- `all-groups-stats.{csv,md}` - 群組資料 + 成員統計
- `all-groups-stats-subgroups.{csv,md}` - 子群組資料
- `all-groups-stats-projects.{csv,md}` - 群組專案資料
- `all-groups-stats-permissions.{csv,md}` - 授權詳細資料

**群組統計欄位：**
- 群組基本資訊：`group_name`, `description`, `visibility`, `created_at`, `web_url`
- 成員統計：`total_members`, `owners`, `maintainers`, `developers`, `reporters`, `guests`
- 資源統計：`subgroups_count`, `projects_count`

**授權詳細資料包含：**
- 群組成員授權
- 群組內所有專案的成員授權
- 支援使用者和群組類型的授權

---

### 3️⃣ 專案授權查詢 (`project-permission`) ⚠️ **已棄用**

> **⚠️ 警告：此命令已棄用，建議使用 `project-stats`**
>
> `project-stats` 已包含完整的授權資訊（統計 + 詳細資料），此命令僅為向下相容保留。

```bash
# ❌ 不建議使用（僅為向下相容）
uv run python gl-cli.py project-permission

# ✅ 建議使用（功能更完整）
uv run python gl-cli.py project-stats
```

**輸出:** `./output/all-project-permission.{csv,md}`

---

### 4️⃣ 使用者統計查詢 (`user-stats`)
深度分析開發者活動：commits、MR、code review、授權、統計

```bash
# 分析 2024 年所有開發者（包含授權資訊）
uv run python gl-cli.py user-stats --start-date 2024-01-01 --end-date 2024-12-31

# 分析特定開發者（包含授權資訊）
uv run python gl-cli.py user-stats --username alice --start-date 2024-01-01
```

**輸出檔案:** 
- `commits.{csv,md}` - Commit 記錄
- `merge_requests.{csv,md}` - MR 資料
- `code_reviews.{csv,md}` - Code Review
- `permissions.{csv,md}` - **授權資訊** 🆕
- `statistics.{csv,md}` - **統計摘要**（包含授權統計）⭐

**授權統計欄位（新增）：**
- `total_projects_with_access` - 有授權的專案總數
- `owner_projects` - Owner 權限專案數
- `maintainer_projects` - Maintainer 權限專案數
- `developer_projects` - Developer 權限專案數
- `reporter_projects` - Reporter 權限專案數
- `guest_projects` - Guest 權限專案數

---

## 🛠️ 便捷腳本（推薦）

### Linux/macOS:
```bash
./run-gl-cli.sh project-stats
./run-gl-cli.sh user-stats --start-date 2024-01-01
```

### Windows (PowerShell):
```powershell
.\run-gl-cli.ps1 project-stats
.\run-gl-cli.ps1 user-stats --start-date 2024-01-01
```

---

## ✨ 核心特色

- ✅ **SOLID 原則** - 單一職責、開放封閉、里氏替換、介面隔離、依賴反轉
- ✅ **雙格式輸出** - CSV (Excel) + Markdown (報告)
- ✅ **深度分析** - Commits、Code Changes、MR、Code Review、統計
- ✅ **彈性查詢** - 全部/特定專案、全部/特定使用者、時間範圍
- ✅ **跨平台** - Linux/macOS/Windows 都支援
- ✅ **便捷腳本** - Shell + PowerShell

---

## 📊 分析指標

### 程式碼品質
- ✅ Commit 頻率與規律性
- ✅ 程式碼變更量分布（粒度）
- ✅ 新增/刪除/重構比例

### 技術水平
- ✅ 參與專案數量與範圍
- ✅ 程式碼重構能力
- ✅ 團隊協作能力（MR、Code Review）
- ✅ 程式碼穩定性（MR 合併率）

---

## 💡 實用範例

### 範例 1: 快速盤點所有專案（含授權統計）
```bash
# 取得所有專案資訊（已驗證：成功獲取 378 個專案 + 授權資訊）
uv run python gl-cli.py project-stats

# 輸出檔案（4 個）
# - output/all-project-stats.csv (包含授權統計)
# - output/all-project-stats.md
# - output/all-project-stats-permissions.csv (授權詳細資料)
# - output/all-project-stats-permissions.md
```

**專案資料包含的授權統計（新增 8 個欄位）：**
- `total_members` - 總成員數（快速識別成員過多/過少的專案）
- `user_members` - 使用者成員數
- `group_members` - 群組成員數
- `owners` - Owner 等級人數（風險指標：過多表示權限管理不當）
- `maintainers` - Maintainer 等級人數
- `developers` - Developer 等級人數
- `reporters` - Reporter 等級人數
- `guests` - Guest 等級人數

**授權詳細資料包含：**
- 每個成員的名稱、帳號、權限等級
- User 和 Group 類型區分
- 可用於權限審計、合規性檢查

**實際用途：**
- 📊 專案清單總覽 + 成員統計
- 🔍 找出長時間未更新的專案
- 📈 統計 public/private 專案比例
- 👥 識別成員配置異常的專案（過多 Owner、無 Developer 等）
- 🔒 權限風險分析（Owner/Maintainer 過多）

---

### 範例 2: 群組資訊查詢與授權審計
```bash
# 取得所有群組資訊（包含子群組、專案、授權）
uv run python gl-cli.py group-stats

# 產生檔案（4 個）：
# - output/all-groups-stats.csv (群組基本資訊 + 成員統計)
# - output/all-groups-stats.md
# - output/all-groups-stats-subgroups.csv (子群組資料)
# - output/all-groups-stats-subgroups.md
# - output/all-groups-stats-projects.csv (群組內專案資料)
# - output/all-groups-stats-projects.md
# - output/all-groups-stats-permissions.csv (授權詳細資料)
# - output/all-groups-stats-permissions.md
```

**群組資料包含：**
- `group_name`, `group_path`, `group_full_path` - 群組識別資訊
- `description`, `visibility`, `created_at` - 群組基本資訊
- `total_members` - 總成員數
- `owners`, `maintainers`, `developers`, `reporters`, `guests` - 各權限等級人數
- `subgroups_count`, `projects_count` - 資源統計

**子群組資料包含：**
- 父群組與子群組的關聯
- 子群組的完整路徑、描述、可見性

**專案資料包含：**
- 所屬群組資訊
- 專案基本資訊、活動時間、URL

**授權詳細資料包含：**
- 群組成員授權（resource_type: Group）
- 專案成員授權（resource_type: Project）
- 成員名稱、帳號、權限等級、過期時間

**實際用途：**
- 📊 群組架構總覽（包含子群組層級）
- 👥 群組成員配置分析
- 🔍 找出無人維護的群組
- 🔒 群組權限審計（跨群組和專案）
- 📈 統計群組資源配置（專案數、成員數）

**範例分析：**
```bash
# 在 Excel 中開啟 all-groups-stats.csv
# 使用篩選功能：
# - owners > 3：找出 Owner 過多的群組（風險）
# - projects_count = 0：找出無專案的空群組
# - total_members < 2：找出成員不足的群組
```

---

### 範例 3: 查詢特定群組
```bash
# 使用群組名稱搜尋（模糊匹配）
uv run python gl-cli.py group-stats --group-name "backend"

# 輸出: backend-group-stats.csv, backend-group-stats-permissions.csv 等
```

**適用場景：**
- 檢查特定群組的詳細資訊
- 驗證群組設定是否正確
- 單一群組的權限審計

---

### 範例 4: 查詢特定專案
```bash
# 使用專案名稱搜尋（模糊匹配）
uv run python gl-cli.py project-stats --project-name "web-component"

# 輸出: web-component-project-stats.csv
```

**適用場景：**
- 檢查特定專案的詳細資訊
- 驗證專案設定是否正確

---

### 範例 5: 專案權限審計與成員分析
```bash
# 方式 1: 使用 project-stats（推薦，一次獲取專案資料 + 授權）
uv run python gl-cli.py project-stats

# 產生檔案：
# - all-project-stats.csv（包含授權統計欄位）
# - all-project-stats-permissions.csv（授權詳細資料）

# 方式 2: 使用 project-permission（只獲取授權資訊）
uv run python gl-cli.py project-permission

# 產生檔案：
# - all-project-permission.csv
```

**授權統計欄位說明（project-stats 輸出）：**
```csv
project_name,total_members,owners,maintainers,developers,...
web-app,15,1,2,12,...
api-server,8,2,1,5,...
```

**授權詳細資料（permissions 檔案）：**
```csv
project_name,member_name,member_username,access_level_name
web-app,張三,user1,Developer
web-app,李四,user2,Maintainer
```

**實際用途：**
- 🔒 **權限審計**：找出不應有存取權的人
- 👥 **成員盤點**：了解每個專案的團隊組成
- 📋 **合規性檢查**：確保離職人員已移除權限
- ⚠️ **風險識別**：找出 Owner/Maintainer 過多的專案
- 📊 **團隊分析**：統計各專案的開發人力配置

**範例分析：**
```bash
# 在 Excel 中開啟 all-project-stats.csv
# 使用篩選功能：
# - owners > 2：找出 Owner 過多的專案（風險）
# - total_members = 0：找出無人維護的專案
# - developers < 2：找出開發人力不足的專案
```

---

### 範例 6: 評估開發者績效（年度報告）
```bash
# 分析特定開發者 2024 年的表現
uv run python gl-cli.py user-stats --username alice --start-date 2024-01-01 --end-date 2024-12-31

# 產生 5 個檔案
# alice-user-commits.csv        - 所有 commit 記錄
# alice-user-code_changes.csv   - 程式碼異動詳情
# alice-user-merge_requests.csv - MR 資料
# alice-user-code_reviews.csv   - Code Review 參與
# alice-user-statistics.csv     - 統計摘要 ⭐
```

**關鍵指標 (statistics.csv)：**
```
total_commits            : 總 commit 數（活躍度）
total_additions          : 新增行數（貢獻量）
avg_changes_per_commit   : 平均每次變更量（建議 100-500）
total_merge_requests     : 總 MR 數（流程遵循）
merged_mrs               : 已合併 MR（品質指標）
total_code_reviews       : Code Review 參與（協作能力）
projects_contributed     : 貢獻專案數（技術廣度）
```

**績效評估標準：**
- 🟢 優秀：avg_changes 100-500、高 MR 合併率、積極參與 review
- 🟡 中等：commits 穩定、有 MR、偶爾 review
- 🔴 需改進：commits 少、無 MR、不參與 review

---

### 範例 7: 團隊月度報告
```bash
# 分析團隊 2024 年 1 月的活動
uv run python gl-cli.py user-stats --start-date 2024-01-01 --end-date 2024-01-31

# 輸出
# all-users-statistics.csv  - 可直接放入月報
```

**報告內容可包含：**
- 📊 Top 10 最活躍開發者
- 📈 團隊總 commits、MR、code review 數
- 🎯 平均程式碼品質指標

---

### 範例 8: 批次分析多位開發者
```bash
# Linux/macOS
cat > users.txt << EOF
alice
bob
charlie
david
EOF

while read username; do
  echo "分析: $username"
  uv run python gl-cli.py user-stats --username "$username" --start-date 2024-01-01
done < users.txt
```

```powershell
# Windows (PowerShell)
@"
alice
bob
charlie
david
"@ | Out-File -FilePath users.txt -Encoding UTF8

Get-Content users.txt | ForEach-Object {
    Write-Host "分析: $_"
    uv run python gl-cli.py user-stats --username $_
}
```

---

### 範例 9: 專案群組分析
```bash
# 只分析特定群組的專案（例如 group_id = 123）
uv run python gl-cli.py project-stats --group-id 123
uv run python gl-cli.py user-stats --group-id 123 --start-date 2024-01-01
```

---

### 範例 10: 隱藏 SSL 警告（Self-hosted GitLab）
```bash
# 方法 1: 環境變數
export PYTHONWARNINGS="ignore:Unverified HTTPS request"
uv run python gl-cli.py project-stats

# 方法 2: 在 gitlab_client.py 開頭添加
# import urllib3
# urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
```

---

## 🔧 快速疑難排解

| 問題 | 解決方法 |
|------|----------|
| `ModuleNotFoundError` | `uv sync` |
| `401 Unauthorized` | 檢查 `config.py` 的 `GITLAB_TOKEN` |
| `No projects found` | 檢查專案名稱、權限、群組 ID |
| 輸出檔案太大 | 縮小時間範圍或限制專案/使用者 |

詳細疑難排解請參考 [GL-CLI-README.md](./GL-CLI-README.md#-疑難排解)

---

## 📁 檔案說明

| 檔案 | 說明 |
|------|------|
| `gl-cli.py` ⭐ | 主程式（推薦使用） |
| `run-gl-cli.sh` | Linux/macOS 便捷腳本 |
| `run-gl-cli.ps1` | Windows 便捷腳本 |
| `config.example.py` | 配置範本 |
| **QUICKSTART.md** | 5 分鐘快速開始 |
| **GL-CLI-README.md** | 完整詳細文件 |

---

## ❓ 常見問題

**Q: 如何開始？**  
A: 閱讀 [QUICKSTART.md](./QUICKSTART.md)，5 分鐘即可開始。

**Q: 如何只查詢特定時間？**  
A: 使用 `--start-date 2024-01-01 --end-date 2024-01-31`

**Q: 如何分析程式碼品質？**  
A: 查看 `statistics.csv` 的指標，參考 [分析指標說明](./GL-CLI-README.md#-分析指標說明)

**Q: 看到很多 `InsecureRequestWarning` 警告？**  
A: 這是因為使用 Self-hosted GitLab 的自簽憑證。不影響功能，可用以下方式隱藏：
```bash
export PYTHONWARNINGS="ignore:Unverified HTTPS request"
uv run python gl-cli.py project-stats
```

**Q: 成功執行後輸出在哪裡？**  
A: 所有輸出都在 `./output/` 目錄，包含 `.csv` 和 `.md` 兩種格式。

**Q: CSV 和 Markdown 有什麼差別？**  
A: 
- **CSV**: 可用 Excel 開啟，適合進一步分析、篩選、統計
- **Markdown**: 可直接閱讀，適合報告、文件、分享

**Q: 實際測試結果如何？**  
A: 已在實際環境測試：
- ✅ 成功獲取 378 個專案資訊
- ✅ 生成 115 KB CSV + 315 KB Markdown
- ✅ 包含完整欄位（專案名稱、描述、URL、統計數據等）

更多問題請參考 [完整文件 FAQ](./GL-CLI-README.md#-常見問題-faq)

---

## 🎉 立即開始

```bash
# 1. 閱讀快速開始
cat QUICKSTART.md

# 2. 安裝依賴
uv sync

# 3. 設定 config.py
# (複製 config.example.py 並填入你的設定)

# 4. 執行第一個命令
uv run python gl-cli.py project-stats

# 5. 檢查輸出
ls -lh output/
```

**執行結果示範：**
```
======================================================================
GitLab 專案資訊查詢
======================================================================
✓ CSV exported: output/all-project-stats.csv
✓ Markdown exported: output/all-project-stats.md

✓ Total projects: 378
======================================================================

輸出檔案：
-rwxrwxrwx 1 user user 115K Jan 15 12:04 all-project-stats.csv
-rwxrwxrwx 1 user user 315K Jan 15 12:04 all-project-stats.md
```

**祝分析愉快！** 🚀

---

**版本:** 1.0.0  
**最後更新:** 2026-01-15  
**授權:** 僅供學習與內部使用
