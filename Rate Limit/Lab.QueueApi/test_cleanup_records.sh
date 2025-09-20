#!/bin/bash

# 測試清理記錄功能的腳本

echo "======================================"
echo "測試清理記錄功能"
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

    if [ "$http_code" = "200" ] || [ "$http_code" = "202" ] || [ "$http_code" = "429" ]; then
        request_id=$(echo "$body" | jq -r '.requestId // .id // empty')
        if [ -n "$request_id" ] && [ "$request_id" != "null" ]; then
            echo "✓ 請求成功，ID: $request_id，HTTP 狀態: $http_code"
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

# 函數：檢查清理記錄
check_cleanup_records() {
    echo "檢查清理記錄："

    response=$(curl -s "$BASE_URL/api/commands/cleanup-summary" -w "\n%{http_code}")
    http_code=$(echo "$response" | tail -1)
    body=$(echo "$response" | head -n -1)

    echo "HTTP 狀態碼: $http_code"
    if [ "$http_code" = "200" ]; then
        echo "清理記錄摘要："
        echo "$body" | jq '.'
    else
        echo "取得清理記錄失敗: $body"
    fi
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

echo "請確保 API 伺服器在 $BASE_URL 上運行"
echo ""

echo "1. 檢查初始清理記錄狀態"
check_cleanup_records

echo "2. 發送測試請求到佇列中"
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
request3=$(send_request "測試清理記錄 3")
echo ""

# 發送第四個請求（應該進入佇列）
echo "發送第四個請求（應該進入佇列）..."
request4=$(send_request "測試清理記錄 4")
echo ""

echo "3. 列出目前佇列狀態"
list_queue

echo "4. 等待清理機制運行（為了測試，我們會等待一段時間）"
echo "注意：預設清理時間為 5 分鐘，清理服務每 1 分鐘運行一次"
echo ""

echo "為了快速測試，建議您可以："
echo "1. 修改 ExpiredRequestCleanupService 建構函式中的參數："
echo "   - maxRequestAge: TimeSpan.FromSeconds(30) // 30 秒後過期"
echo "   - cleanupInterval: TimeSpan.FromSeconds(10) // 每 10 秒清理一次"
echo "2. 重新建置並啟動應用程式"
echo "3. 再次執行此測試腳本"
echo ""

# 選項：等待較短時間進行示範
echo "現在等待 2 分鐘來示範清理記錄的收集..."
echo "（在實際測試中，您可能需要等待更長時間或修改清理設定）"

for i in {1..120}; do
    if [ $((i % 30)) -eq 0 ]; then
        echo "已等待 $((i / 2)) 分鐘..."
        echo "檢查清理記錄更新："
        check_cleanup_records
    fi
    sleep 1
done

echo ""
echo "5. 最終檢查清理記錄"
check_cleanup_records

echo "6. 檢查佇列狀態"
list_queue

echo "======================================"
echo "測試完成！"
echo ""
echo "期待的行為："
echo "- 新的 /api/commands/cleanup-summary 端點應該回傳清理記錄摘要"
echo "- 清理記錄應該包含被清理請求的詳細資訊"
echo "- 清理記錄應該顯示請求存活時間和清理原因"
echo "- 清理記錄應該按清理時間倒序排列"
echo ""
echo "清理記錄格式範例："
echo "{"
echo "  \"totalCleanupCount\": 2,"
echo "  \"cleanupRecords\": ["
echo "    {"
echo "      \"requestId\": \"abc-123\","
echo "      \"requestData\": \"測試清理記錄 3\","
echo "      \"queuedAt\": \"2025-01-XX\","
echo "      \"cleanedAt\": \"2025-01-XX\","
echo "      \"lifeSpan\": \"00:05:00\","
echo "      \"reason\": \"Expired after 5.0 minutes\""
echo "    }"
echo "  ],"
echo "  \"lastCleanupTime\": \"2025-01-XX\","
echo "  \"maxRecordsKept\": 1000"
echo "}"
echo "======================================"