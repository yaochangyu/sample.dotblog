#!/bin/bash

# 測試案例 2.3: User-Agent 不一致

echo "======================================"
echo "測試案例 2.3: User-Agent 不一致"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"
USER_AGENT_A="Mozilla/5.0 (TestClient-A)"
USER_AGENT_B="Mozilla/5.0 (TestClient-B)"

echo ""
echo "步驟 1: 使用 User-Agent A 取得 Token"
echo "--------------------------------------"
echo "User-Agent: $USER_AGENT_A"

RESPONSE=$(curl -s -i -A "$USER_AGENT_A" "${API_BASE}/api/token?maxUsage=5&expirationMinutes=5")
TOKEN=$(echo "$RESPONSE" | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

if [ -z "$TOKEN" ]; then
    echo "❌ 失敗: 無法取得 Token"
    exit 1
fi

echo "✅ Token 取得成功: $TOKEN"

echo ""
echo "步驟 2: 使用 User-Agent B 呼叫 Protected API"
echo "--------------------------------------"
echo "User-Agent: $USER_AGENT_B"

API_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -A "$USER_AGENT_B" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -H "X-CSRF-Token: $TOKEN" \
    -d '{"data":"測試 User-Agent 不一致"}')

HTTP_STATUS=$(echo "$API_RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY=$(echo "$API_RESPONSE" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $HTTP_STATUS"
echo "回應內容: $BODY"

if [ "$HTTP_STATUS" = "403" ]; then
    echo ""
    echo "✅ 測試通過: User-Agent 不一致的請求被正確拒絕 (HTTP 403)"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: 預期 HTTP 403，實際 HTTP $HTTP_STATUS"
    exit 1
fi
