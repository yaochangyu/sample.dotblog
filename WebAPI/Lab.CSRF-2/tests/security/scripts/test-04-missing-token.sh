#!/bin/bash

# 測試案例 2.1: 無 Token 請求

echo "======================================"
echo "測試案例 2.1: 無 Token 請求"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"

echo ""
echo "步驟: 直接呼叫 Protected API 不帶 Token"
echo "--------------------------------------"

RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -d '{"data":"測試無 Token 請求"}')

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $HTTP_STATUS"
echo "回應內容: $BODY"

if [ "$HTTP_STATUS" = "403" ]; then
    echo ""
    echo "✅ 測試通過: 無 Token 請求被正確拒絕 (HTTP 403)"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: 預期 HTTP 403，實際 HTTP $HTTP_STATUS"
    exit 1
fi
