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

## 🎯 三大核心功能

### 1️⃣ 專案資訊查詢 (`project-stats`)
查詢專案基本資料、活動狀態、統計數據

```bash
# 所有專案
uv run python gl-cli.py project-stats

# 特定專案
uv run python gl-cli.py project-stats --project-name "web-app"
```

**輸出:** `./output/all-project-stats.{csv,md}`

---

### 2️⃣ 專案授權查詢 (`project-permission`)
查詢專案成員、群組權限、存取等級

```bash
# 所有專案授權
uv run python gl-cli.py project-permission

# 特定專案授權
uv run python gl-cli.py project-permission --project-name "web-app"
```

**輸出:** `./output/all-project-permission.{csv,md}`

---

### 3️⃣ 使用者統計查詢 (`user-stats`)
深度分析開發者活動：commits、MR、code review、統計

```bash
# 分析 2024 年所有開發者
uv run python gl-cli.py user-stats --start-date 2024-01-01 --end-date 2024-12-31

# 分析特定開發者
uv run python gl-cli.py user-stats --username alice
```

**輸出:** 
- `commits.{csv,md}` - Commit 記錄
- `merge_requests.{csv,md}` - MR 資料
- `code_reviews.{csv,md}` - Code Review
- `statistics.{csv,md}` - **統計摘要** ⭐

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

### 範例 1: 評估開發者績效
```bash
uv run python gl-cli.py user-stats --username alice --start-date 2024-01-01
```
查看 `output/alice-user-statistics.csv` 的關鍵指標。

### 範例 2: 專案健康度檢查
```bash
uv run python gl-cli.py project-stats
uv run python gl-cli.py project-permission
```
檢查專案活躍度、待處理問題、存取權限。

### 範例 3: 團隊月度報告
```bash
uv run python gl-cli.py user-stats --start-date 2024-01-01 --end-date 2024-01-31
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

**祝分析愉快！** 🚀

---

**版本:** 1.0.0  
**最後更新:** 2026-01-15  
**授權:** 僅供學習與內部使用
