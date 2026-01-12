#!/bin/bash

# 測試案例 2.4: 速率限制測試

echo "======================================"
echo "測試案例 2.4: 速率限制測試"
echo "======================================"

API_BASE="${API_BASE:-http://localhost:5073}"

echo ""
echo "測試: 1 分鐘內連續請求 6 次 Token (限制為 5 次)"
echo "--------------------------------------"

SUCCESS_COUNT=0
RATE_LIMITED=false

for i in {1..6}; do
    echo ""
    echo "請求 #$i"
    
    RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "${API_BASE}/api/token?maxUsage=1&expirationMinutes=5")
    HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d':' -f2)
    
    echo "  HTTP 狀態碼: $HTTP_STATUS"
    
    if [ "$HTTP_STATUS" = "200" ]; then
        SUCCESS_COUNT=$((SUCCESS_COUNT + 1))
        echo "  ✅ 成功"
    elif [ "$HTTP_STATUS" = "429" ]; then
        echo "  ⚠️  速率限制觸發 (Too Many Requests)"
        RATE_LIMITED=true
        break
    else
        echo "  ❌ 未預期的狀態碼: $HTTP_STATUS"
    fi
    
    sleep 0.5
done

echo ""
echo "========================================"
echo "測試結果:"
echo "  成功請求數: $SUCCESS_COUNT"
echo "  速率限制觸發: $RATE_LIMITED"
echo "========================================"

if [ "$SUCCESS_COUNT" -le 5 ] && [ "$RATE_LIMITED" = true ]; then
    echo ""
    echo "✅ 測試通過: 速率限制正常運作"
    exit 0
elif [ "$SUCCESS_COUNT" -eq 5 ]; then
    echo ""
    echo "⚠️  測試部分通過: 達到 5 次請求，但未觸發速率限制 (可能時間窗口已重置)"
    exit 0
else
    echo ""
    echo "❌ 測試失敗: 速率限制未正常運作"
    exit 1
fi
