#!/bin/bash

# 測試案例 1.2: Token 過期測試

echo "======================================"
echo "測試案例 1.2: Token 過期測試"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"

echo ""
echo "步驟 1: 取得 Token (設定 1 分鐘過期)"
echo "--------------------------------------"

RESPONSE=$(curl -s -i "${API_BASE}/api/token?maxUsage=5&expirationMinutes=1")
TOKEN=$(echo "$RESPONSE" | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

if [ -z "$TOKEN" ]; then
    echo "❌ 失敗: 無法取得 Token"
    exit 1
fi

echo "✅ Token 取得成功: $TOKEN"

echo ""
echo "步驟 2: 等待 61 秒讓 Token 過期..."
echo "--------------------------------------"

for i in {61..1}; do
    printf "\r剩餘時間: %02d 秒" $i
    sleep 1
done
echo ""

echo ""
echo "步驟 3: 使用過期 Token 呼叫 API"
echo "--------------------------------------"

API_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -H "X-CSRF-Token: $TOKEN" \
    -d '{"data":"測試過期 Token"}')

HTTP_STATUS=$(echo "$API_RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY=$(echo "$API_RESPONSE" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $HTTP_STATUS"
echo "回應內容: $BODY"

if [ "$HTTP_STATUS" = "403" ]; then
    echo ""
    echo "✅ 測試通過: 過期 Token 被正確拒絕 (HTTP 403)"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: 預期 HTTP 403，實際 HTTP $HTTP_STATUS"
    exit 1
fi
