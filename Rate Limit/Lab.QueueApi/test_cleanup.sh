#!/bin/bash

# 測試過期請求清理機制的腳本

echo "======================================"
echo "測試過期請求清理機制"
echo "======================================"

# API 基本 URL
BASE_URL="http://localhost:5001"

# 函數：發送 POST 請求並回傳 request ID
send_request() {
    local data=$1
    echo "發送請求: $data"

    response=$(curl -s -X POST "$BASE_URL/api/commands" \
        -H "Content-Type: application/json" \
        -d '{"data": "'"$data"'"}' \
        -w "\n%{http_code}")

    http_code=$(echo "$response" | tail -1)
    body=$(echo "$response" | head -n -1)

    if [ "$http_code" = "200" ] || [ "$http_code" = "202" ]; then
        request_id=$(echo "$body" | jq -r '.requestId // .id // empty')
        if [ -n "$request_id" ] && [ "$request_id" != "null" ]; then
            echo "✓ 請求成功，ID: $request_id"
            echo "$request_id"
        else
            echo "✗ 無法取得請求 ID"
            echo "回應: $body"
        fi
    else
        echo "✗ 請求失敗，HTTP 狀態碼: $http_code"
        echo "回應: $body"
    fi
}

# 函數：檢查請求狀態
check_status() {
    local request_id=$1
    echo "檢查請求狀態: $request_id"

    response=$(curl -s "$BASE_URL/api/commands/$request_id/status" -w "\n%{http_code}")
    http_code=$(echo "$response" | tail -1)
    body=$(echo "$response" | head -n -1)

    echo "HTTP 狀態碼: $http_code"
    echo "回應: $body"
    echo ""
}

# 函數：列出所有佇列中的請求
list_queue() {
    echo "列出所有佇列中的請求："

    response=$(curl -s "$BASE_URL/api/commands" -w "\n%{http_code}")
    http_code=$(echo "$response" | tail -1)
    body=$(echo "$response" | head -n -1)

    echo "HTTP 狀態碼: $http_code"
    if [ "$http_code" = "200" ]; then
        echo "佇列中的請求："
        echo "$body" | jq '.'
    else
        echo "取得佇列失敗: $body"
    fi
    echo ""
}

echo "1. 啟動 API 伺服器（在背景執行）"
echo "請確保 API 伺服器在 $BASE_URL 上運行"
echo ""

echo "2. 發送一些測試請求到佇列中"
echo ""

# 發送第一個請求（應該直接處理）
echo "發送第一個請求..."
request1=$(send_request "測試請求 1")
echo ""

# 發送第二個請求（應該直接處理）
echo "發送第二個請求..."
request2=$(send_request "測試請求 2")
echo ""

# 發送第三個請求（應該進入佇列）
echo "發送第三個請求（應該進入佇列）..."
request3=$(send_request "測試請求 3")
echo ""

# 發送第四個請求（應該進入佇列）
echo "發送第四個請求（應該進入佇列）..."
request4=$(send_request "測試請求 4")
echo ""

echo "3. 列出目前佇列狀態"
list_queue

echo "4. 檢查請求狀態"
if [ -n "$request3" ]; then
    check_status "$request3"
fi
if [ -n "$request4" ]; then
    check_status "$request4"
fi

echo "5. 等待 6 分鐘，讓清理服務運行（模擬超過 5 分鐘的過期時間）"
echo "清理服務設定為每 1 分鐘運行一次，會清理超過 5 分鐘的請求"
echo "為了測試，我們將等待足夠長的時間..."
echo ""
echo "注意：在實際環境中，您可以修改 ExpiredRequestCleanupService 的設定"
echo "將清理時間改為較短的時間進行測試（例如 30 秒）"
echo ""
echo "等待中..."

# 等待 6 分鐘（360 秒）
for i in {1..360}; do
    if [ $((i % 60)) -eq 0 ]; then
        echo "已等待 $((i / 60)) 分鐘..."
    fi
    sleep 1
done

echo ""
echo "6. 等待完成，再次檢查佇列狀態"
list_queue

echo "7. 檢查之前的請求是否仍然存在"
if [ -n "$request3" ]; then
    echo "檢查請求 3 的狀態："
    check_status "$request3"
fi
if [ -n "$request4" ]; then
    echo "檢查請求 4 的狀態："
    check_status "$request4"
fi

echo "======================================"
echo "測試完成！"
echo ""
echo "預期結果："
echo "- 如果清理機制正常運作，超過 5 分鐘的請求應該被清理"
echo "- 檢查請求狀態時應該回傳 'Request not found' 或類似訊息"
echo "- 佇列中應該沒有過期的請求"
echo "======================================"