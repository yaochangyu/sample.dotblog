#!/bin/bash

# 測試案例 1.3: Token 使用次數限制

echo "======================================"
echo "測試案例 1.3: Token 使用次數限制"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"
USER_AGENT="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"

echo ""
echo "步驟 1: 取得 Token (最大使用次數 = 1)"
echo "--------------------------------------"

RESPONSE=$(curl -s -i \
    -H "User-Agent: ${USER_AGENT}" \
    -H "Referer: ${API_BASE}/" \
    "${API_BASE}/api/token?maxUsage=1&expirationMinutes=5")
TOKEN=$(echo "$RESPONSE" | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

if [ -z "$TOKEN" ]; then
    echo "❌ 失敗: 無法取得 Token"
    exit 1
fi

echo "✅ Token 取得成功: $TOKEN"

echo ""
echo "步驟 2: 第一次呼叫 Protected API (應該成功)"
echo "--------------------------------------"

RESPONSE1=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -H "User-Agent: ${USER_AGENT}" \
    -H "Referer: ${API_BASE}/" \
    -H "X-CSRF-Token: $TOKEN" \
    -d '{"data":"第一次呼叫"}')

STATUS1=$(echo "$RESPONSE1" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY1=$(echo "$RESPONSE1" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $STATUS1"
echo "回應內容: $BODY1"

if [ "$STATUS1" != "200" ]; then
    echo "❌ 第一次呼叫失敗"
    exit 1
fi

echo "✅ 第一次呼叫成功"

echo ""
echo "步驟 3: 第二次呼叫 Protected API (應該失敗)"
echo "--------------------------------------"

RESPONSE2=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -H "User-Agent: ${USER_AGENT}" \
    -H "Referer: ${API_BASE}/" \
    -H "X-CSRF-Token: $TOKEN" \
    -d '{"data":"第二次呼叫"}')

STATUS2=$(echo "$RESPONSE2" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY2=$(echo "$RESPONSE2" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $STATUS2"
echo "回應內容: $BODY2"

# 接受 401 (Token 失效) 或 403 (Forbidden) 都算通過
if [ "$STATUS2" = "401" ] || [ "$STATUS2" = "403" ]; then
    echo ""
    echo "✅ 測試通過: 使用次數限制正常運作"
    echo "   - 第一次呼叫: HTTP $STATUS1 (成功)"
    echo "   - 第二次呼叫: HTTP $STATUS2 (被拒絕)"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: 第二次呼叫應該被拒絕 (401 或 403)，但得到 HTTP $STATUS2"
    exit 1
fi
