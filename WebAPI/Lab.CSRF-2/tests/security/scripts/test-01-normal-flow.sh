#!/bin/bash

# 測試案例 1.1: 正常 Token 取得與使用

echo "======================================"
echo "測試案例 1.1: 正常 Token 取得與使用"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"

echo ""
echo "步驟 1: 取得 Token"
echo "--------------------------------------"

RESPONSE=$(curl -s -i "${API_BASE}/api/token?maxUsage=1&expirationMinutes=5")

# 提取 Token
TOKEN=$(echo "$RESPONSE" | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

if [ -z "$TOKEN" ]; then
    echo "❌ 失敗: 無法取得 Token"
    echo "$RESPONSE"
    exit 1
fi

echo "✅ Token 取得成功: $TOKEN"

echo ""
echo "步驟 2: 使用 Token 呼叫 Protected API"
echo "--------------------------------------"

API_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -H "X-CSRF-Token: $TOKEN" \
    -d '{"data":"測試資料"}')

HTTP_STATUS=$(echo "$API_RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY=$(echo "$API_RESPONSE" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $HTTP_STATUS"
echo "回應內容: $BODY"

if [ "$HTTP_STATUS" = "200" ]; then
    echo ""
    echo "✅ 測試通過: Protected API 呼叫成功"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: 預期 HTTP 200，實際 HTTP $HTTP_STATUS"
    exit 1
fi
