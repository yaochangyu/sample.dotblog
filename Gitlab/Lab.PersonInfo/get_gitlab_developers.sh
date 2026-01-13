#!/bin/bash

# GitLab 開發者資訊取得工具
# 支援多種方式取得 GitLab 上的開發者清單

GITLAB_URL="${1:-https://192.168.1.158}"
GITLAB_TOKEN="${2}"

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║         GitLab 開發者資訊取得工具                            ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# 檢查參數
if [ -z "$GITLAB_TOKEN" ]; then
    echo "⚠️  注意: 未提供 GitLab Token"
    echo ""
    echo "📖 使用方式:"
    echo "   $0 <GitLab URL> <Access Token>"
    echo ""
    echo "範例:"
    echo "   $0 https://192.168.1.158 glpat-xxxxxxxxxxxxx"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "💡 如何取得 GitLab Access Token:"
    echo ""
    echo "1. 登入 GitLab: $GITLAB_URL"
    echo "2. 點選右上角頭像 > Preferences (設定)"
    echo "3. 左側選單選擇 Access Tokens"
    echo "4. 建立新的 Personal Access Token"
    echo "   • Token name: developer-analyzer"
    echo "   • Expiration: 設定過期時間"
    echo "   • Scopes: 勾選以下權限"
    echo "     ✓ read_api"
    echo "     ✓ read_user"
    echo "     ✓ read_repository"
    echo "5. 點選 Create personal access token"
    echo "6. 複製顯示的 token (只會顯示一次)"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "🔄 替代方案: 使用已 clone 的專案分析"
    echo ""
    echo "如果你已經 clone 了 GitLab 專案:"
    echo "   cd /path/to/your/gitlab/project"
    echo "   ../list_developers.sh"
    echo ""
    exit 1
fi

echo "🔍 正在連接 GitLab: $GITLAB_URL"
echo ""

# 測試連線
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "1️⃣  測試 GitLab API 連線..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

response=$(curl -s -k -w "\n%{http_code}" \
    --header "PRIVATE-TOKEN: $GITLAB_TOKEN" \
    "$GITLAB_URL/api/v4/user" 2>/dev/null)

http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" == "200" ]; then
    username=$(echo "$body" | grep -o '"username":"[^"]*' | cut -d'"' -f4)
    echo "✅ 連線成功！當前使用者: $username"
else
    echo "❌ 連線失敗 (HTTP $http_code)"
    echo ""
    echo "可能原因:"
    echo "  • Token 無效或過期"
    echo "  • 網路無法連到 $GITLAB_URL"
    echo "  • Token 權限不足"
    echo ""
    exit 1
fi

echo ""

# 取得所有使用者
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "2️⃣  取得所有 GitLab 使用者..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

users=$(curl -s -k \
    --header "PRIVATE-TOKEN: $GITLAB_TOKEN" \
    "$GITLAB_URL/api/v4/users?per_page=100" 2>/dev/null)

if [ $? -eq 0 ] && [ -n "$users" ]; then
    echo "$users" | python3 -c "
import sys, json
try:
    users = json.load(sys.stdin)
    print(f'\n找到 {len(users)} 位使用者:\n')
    print(f'{'ID':<8} {'使用者名稱':<20} {'姓名':<25} {'Email':<30}')
    print('─' * 85)
    for user in users:
        user_id = str(user.get('id', 'N/A'))
        username = user.get('username', 'N/A')
        name = user.get('name', 'N/A')
        email = user.get('email', 'N/A') or user.get('public_email', 'N/A')
        print(f'{user_id:<8} {username:<20} {name:<25} {email:<30}')
except:
    print('解析使用者資料時發生錯誤')
    sys.exit(1)
"
else
    echo "❌ 無法取得使用者清單"
    exit 1
fi

echo ""

# 取得所有專案
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "3️⃣  取得所有可存取的專案..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

projects=$(curl -s -k \
    --header "PRIVATE-TOKEN: $GITLAB_TOKEN" \
    "$GITLAB_URL/api/v4/projects?per_page=100&membership=true" 2>/dev/null)

if [ $? -eq 0 ] && [ -n "$projects" ]; then
    echo "$projects" | python3 -c "
import sys, json
try:
    projects = json.load(sys.stdin)
    print(f'\n找到 {len(projects)} 個專案:\n')
    print(f'{'ID':<8} {'專案名稱':<40} {'Commits':<10}')
    print('─' * 60)
    for proj in projects:
        proj_id = str(proj.get('id', 'N/A'))
        name = proj.get('name_with_namespace', proj.get('name', 'N/A'))
        commits = str(proj.get('statistics', {}).get('commit_count', 'N/A'))
        print(f'{proj_id:<8} {name:<40} {commits:<10}')
except:
    print('解析專案資料時發生錯誤')
    sys.exit(1)
"
else
    echo "❌ 無法取得專案清單"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ 資料取得完成"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "💡 下一步:"
echo "   1. 選擇要分析的專案"
echo "   2. clone 專案到本地"
echo "   3. 使用 developer_analyzer.py 分析開發者"
echo ""
