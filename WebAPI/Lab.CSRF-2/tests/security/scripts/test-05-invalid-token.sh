#!/bin/bash

# 測試案例 2.2: 無效 Token

echo "======================================"
echo "測試案例 2.2: 無效 Token"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"
FAKE_TOKEN="12345678-1234-1234-1234-123456789abc"

echo ""
echo "步驟: 使用偽造的 Token 呼叫 Protected API"
echo "--------------------------------------"
echo "偽造 Token: $FAKE_TOKEN"

RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -H "X-CSRF-Token: $FAKE_TOKEN" \
    -d '{"data":"測試無效 Token"}')

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $HTTP_STATUS"
echo "回應內容: $BODY"

if [ "$HTTP_STATUS" = "403" ]; then
    echo ""
    echo "✅ 測試通過: 無效 Token 被正確拒絕 (HTTP 403)"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: 預期 HTTP 403，實際 HTTP $HTTP_STATUS"
    exit 1
fi
