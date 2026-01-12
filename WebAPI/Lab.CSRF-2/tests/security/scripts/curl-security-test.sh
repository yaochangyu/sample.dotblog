#!/bin/bash
# API 安全性測試腳本 - 使用 cURL
# 測試 api/protected 端點的 Token 驗證機制

echo "=== API 安全性測試 (cURL 版本) ==="
echo ""

BASE_URL="https://localhost:7001"
TESTS_PASSED=0
TESTS_FAILED=0

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

test_api_endpoint() {
    local test_name="$1"
    local curl_cmd="$2"
    local expected_status="$3"
    local description="$4"
    
    echo -e "${YELLOW}測試: $test_name${NC}"
    echo -e "${GRAY}說明: $description${NC}"
    echo -e "${GRAY}命令: $curl_cmd${NC}"
    
    # 執行 curl 並取得 HTTP 狀態碼
    response=$(eval "$curl_cmd")
    status_code=$(echo "$response" | grep -E "HTTP/[0-9.]+ [0-9]+" | tail -1 | grep -oE "[0-9]{3}" | head -1)
    
    if [ "$status_code" == "$expected_status" ]; then
        echo -e "${GREEN}✓ 測試通過 (狀態: $status_code)${NC}"
        ((TESTS_PASSED++))
    else
        echo -e "${RED}✗ 測試失敗 (預期: $expected_status, 實際: $status_code)${NC}"
        ((TESTS_FAILED++))
    fi
    
    echo ""
    sleep 0.5
}

# 測試 1: 缺少 Token Header
test_api_endpoint \
    "缺少 Token Header - 應拒絕存取" \
    "curl -X POST $BASE_URL/api/protected -H 'Content-Type: application/json' -d '{\"data\":\"測試資料\"}' -k -i -s" \
    "401" \
    "驗證 API 是否拒絕未攜帶 Token 的請求"

# 測試 2: 使用無效 Token
test_api_endpoint \
    "使用無效/偽造的 Token - 應拒絕存取" \
    "curl -X POST $BASE_URL/api/protected -H 'Content-Type: application/json' -H 'X-CSRF-Token: fake-invalid-token-12345' -d '{\"data\":\"測試資料\"}' -k -i -s" \
    "401" \
    "防止攻擊者使用偽造 Token 繞過驗證"

# 測試 3-5: 取得 Token 並測試使用次數
echo -e "${YELLOW}測試: 取得 Token 並測試使用次數限制${NC}"
echo -e "${GRAY}說明: 驗證 Token 正常流程與使用次數機制${NC}"

# 取得 Token
TOKEN=$(curl -X GET "$BASE_URL/api/token?maxUsage=2&expirationMinutes=5" -k -i -s | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r\n')

if [ -z "$TOKEN" ]; then
    echo -e "${RED}✗ 無法取得 Token${NC}"
    ((TESTS_FAILED+=3))
else
    echo -e "${GREEN}✓ Token 取得成功: $TOKEN${NC}"
    echo ""
    
    # 測試 3: 首次使用 Token
    test_api_endpoint \
        "使用有效 Token (首次使用) - 應允許存取" \
        "curl -X POST $BASE_URL/api/protected -H 'Content-Type: application/json' -H 'X-CSRF-Token: $TOKEN' -d '{\"data\":\"測試資料第一次\"}' -k -i -s" \
        "200" \
        "驗證正常的 Token 使用流程"
    
    # 測試 4: 第二次使用 Token
    test_api_endpoint \
        "Token 重複使用 (第二次) - 應允許" \
        "curl -X POST $BASE_URL/api/protected -H 'Content-Type: application/json' -H 'X-CSRF-Token: $TOKEN' -d '{\"data\":\"測試資料第二次\"}' -k -i -s" \
        "200" \
        "驗證 Token 使用次數計數機制 (maxUsage=2)"
    
    # 測試 5: 第三次使用 Token (超過限制)
    test_api_endpoint \
        "Token 超過使用次數限制 (第三次) - 應拒絕" \
        "curl -X POST $BASE_URL/api/protected -H 'Content-Type: application/json' -H 'X-CSRF-Token: $TOKEN' -d '{\"data\":\"測試資料第三次\"}' -k -i -s" \
        "401" \
        "防止 Token 被無限次重複使用"
fi

# 測試 6: 使用過期的 Token
echo -e "${YELLOW}測試: 使用過期的 Token - 應拒絕存取${NC}"
echo -e "${GRAY}說明: 驗證 Token 過期機制${NC}"

# 取得短效 Token (1 秒過期)
EXPIRED_TOKEN=$(curl -X GET "$BASE_URL/api/token?maxUsage=5&expirationMinutes=0.016" -k -i -s | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r\n')

if [ -z "$EXPIRED_TOKEN" ]; then
    echo -e "${RED}✗ 無法取得短效 Token${NC}"
    ((TESTS_FAILED++))
else
    echo -e "${GRAY}等待 Token 過期 (2 秒)...${NC}"
    sleep 2
    
    test_api_endpoint \
        "使用過期 Token - 應拒絕" \
        "curl -X POST $BASE_URL/api/protected -H 'Content-Type: application/json' -H 'X-CSRF-Token: $EXPIRED_TOKEN' -d '{\"data\":\"測試過期Token\"}' -k -i -s" \
        "401" \
        "確保過期 Token 無法被使用"
fi

# 測試 7: 空白 Token Header
test_api_endpoint \
    "空白 Token Header - 應拒絕存取" \
    "curl -X POST $BASE_URL/api/protected -H 'Content-Type: application/json' -H 'X-CSRF-Token: ' -d '{\"data\":\"測試資料\"}' -k -i -s" \
    "401" \
    "防止攻擊者使用空值繞過驗證"

# 測試結果總覽
echo -e "${CYAN}=== 測試結果總覽 ===${NC}"
echo -e "${GREEN}通過: $TESTS_PASSED 項${NC}"
if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}失敗: $TESTS_FAILED 項${NC}"
else
    echo -e "${RED}失敗: $TESTS_FAILED 項${NC}"
fi
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ 所有安全性測試通過！API 受到適當保護。${NC}"
    exit 0
else
    echo -e "${RED}✗ 部分測試失敗，請檢查 API 安全性設定。${NC}"
    exit 1
fi
