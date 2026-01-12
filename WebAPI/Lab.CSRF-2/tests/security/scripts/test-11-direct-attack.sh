#!/bin/bash

# 測試案例 4.1: 直接攻擊 Protected API

echo "======================================"
echo "測試案例 4.1: 直接攻擊 Protected API"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"

echo ""
echo "攻擊場景: 使用 curl 直接攻擊 Protected API"
echo "--------------------------------------"

RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
    -X POST "${API_BASE}/api/protected" \
    -H "Content-Type: application/json" \
    -d '{"data":"直接攻擊測試"}')

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

echo "HTTP 狀態碼: $HTTP_STATUS"
echo "回應內容: $BODY"

if [ "$HTTP_STATUS" = "403" ]; then
    echo ""
    echo "✅ 測試通過: 直接攻擊被成功阻擋 (HTTP 403)"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: API 應該拒絕無 Token 的請求"
    exit 1
fi
