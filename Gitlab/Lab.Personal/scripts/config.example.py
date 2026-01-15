"""
GitLab 分析工具配置檔案範本
請複製此檔案為 config.py 並填入您的設定
"""

# ==================== GitLab 連線設定 ====================

# GitLab 伺服器 URL
# - GitLab.com: "https://gitlab.com"
# - Self-hosted: "https://your-gitlab-server.com"
GITLAB_URL = "https://gitlab.com"

# Personal Access Token
# 建立方式:
#   1. 登入 GitLab
#   2. User Settings → Access Tokens
#   3. 建立 Token，勾選以下權限:
#      - read_api
#      - read_repository
#   4. 複製產生的 Token 貼到這裡
GITLAB_TOKEN = "YOUR_GITLAB_TOKEN_HERE"


# ==================== 分析時間範圍 ====================

# 開始日期 (格式: YYYY-MM-DD)
# 例如: "2024-01-01" 表示從 2024 年 1 月 1 日開始
START_DATE = "2024-01-01"

# 結束日期 (格式: YYYY-MM-DD)
# 例如: "2024-12-31" 表示到 2024 年 12 月 31 日
END_DATE = "2024-12-31"


# ==================== 可選：範圍限制 ====================

# 目標群組 ID (可選)
# - 設定後只會分析該群組下的專案
# - 留空 (None) 則分析所有可存取的專案
# 
# 如何取得群組 ID:
#   1. 前往 GitLab 群組頁面
#   2. Settings → General
#   3. 查看 "Group ID"
#
# 範例: TARGET_GROUP_ID = 123
TARGET_GROUP_ID = None

# 目標專案 ID 列表 (可選)
# - 設定後只會分析這些專案
# - 留空 ([]) 則分析所有可存取的專案
# 
# 如何取得專案 ID:
#   1. 前往 GitLab 專案頁面
#   2. Settings → General
#   3. 查看 "Project ID"
#
# 範例: TARGET_PROJECT_IDS = [456, 789, 101112]
TARGET_PROJECT_IDS = []


# ==================== 輸出設定 ====================

# 輸出目錄
# - 相對路徑: "./output" (預設，在 scripts 目錄下)
# - 絕對路徑: "/path/to/output"
OUTPUT_DIR = "./output"


# ==================== 進階設定 (通常不需要修改) ====================

# SSL 憑證驗證
# - True: 驗證 SSL 憑證 (建議用於 GitLab.com)
# - False: 不驗證 SSL 憑證 (Self-hosted 使用自簽憑證時)
SSL_VERIFY = True

# API 請求逾時時間 (秒)
# - 預設: 30 秒
# - 網路較慢時可增加此值
API_TIMEOUT = 30

# 每頁結果數量
# - 預設: 100 (GitLab API 最大值)
# - 減少此值可降低記憶體使用，但會增加 API 請求次數
PER_PAGE = 100


# ==================== 範例設定 ====================

# 範例 1: GitLab.com 公開專案
# GITLAB_URL = "https://gitlab.com"
# GITLAB_TOKEN = "glpat-xxxxxxxxxxxxxxxxxxxx"
# START_DATE = "2024-01-01"
# END_DATE = "2024-12-31"
# TARGET_GROUP_ID = None
# TARGET_PROJECT_IDS = []

# 範例 2: Self-hosted GitLab (特定群組)
# GITLAB_URL = "https://gitlab.company.com"
# GITLAB_TOKEN = "ypCya3gPCcgh09Qh"
# START_DATE = "2024-01-01"
# END_DATE = "2024-12-31"
# TARGET_GROUP_ID = 123  # 只分析群組 123
# TARGET_PROJECT_IDS = []
# SSL_VERIFY = False  # 使用自簽憑證

# 範例 3: 只分析特定專案
# GITLAB_URL = "https://gitlab.com"
# GITLAB_TOKEN = "glpat-xxxxxxxxxxxxxxxxxxxx"
# START_DATE = "2024-01-01"
# END_DATE = "2024-12-31"
# TARGET_GROUP_ID = None
# TARGET_PROJECT_IDS = [456, 789, 101112]  # 只分析這 3 個專案

# 範例 4: 短期分析 (單月)
# GITLAB_URL = "https://gitlab.com"
# GITLAB_TOKEN = "glpat-xxxxxxxxxxxxxxxxxxxx"
# START_DATE = "2024-01-01"
# END_DATE = "2024-01-31"  # 只分析 1 月份
# TARGET_GROUP_ID = None
# TARGET_PROJECT_IDS = []
