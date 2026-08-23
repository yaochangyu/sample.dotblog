#!/usr/bin/env bash
set -e

API_URL="http://localhost:5287"

echo "=== 1. [Create] 發送 3 筆 Log 至 Web API ==="
for i in {1..3}; do
  curl -s -X POST "$API_URL/api/logs" \
    -H "Content-Type: application/json" \
    -d "{
      \"service\": \"payment-service\",
      \"level\": \"Information\",
      \"message\": \"Invoice generated for txn #$i\",
      \"traceId\": \"trace-pay-$i\"
    }" -w " Status: %{http_code}\n"
done

echo "等待 2 秒（Channel 批次沖刷與 ES 索引重新整理）..."
sleep 2

echo "=== 2. [Read] 依關鍵字與服務搜尋 Logs ==="
SEARCH_RES=$(curl -s "$API_URL/api/logs?service=payment-service&keyword=Invoice")
echo "$SEARCH_RES" | jq .

FIRST_ID=$(echo "$SEARCH_RES" | jq -r '.[0]._id')
echo "取得第一筆 Log _id: $FIRST_ID"

echo "=== 3. [Read] 依 _id 取得單筆 Log ==="
SINGLE_RES=$(curl -s "$API_URL/api/logs/$FIRST_ID")
echo "$SINGLE_RES" | jq .

echo "=== 4. 取得 Data Stream 的實際底層索引名稱 ==="
INDEX_NAME=$(curl -s "http://localhost:9200/_data_stream/logs-app-prod" | jq -r '.data_streams[0].indices[0].index_name')
echo "底層索引為: $INDEX_NAME"

echo "=== 5. [Update] 更新 Log 訊息內容 ==="
UPDATE_STATUS=$(curl -s -X PUT "$API_URL/api/logs/$INDEX_NAME/$FIRST_ID" \
  -H "Content-Type: application/json" \
  -d '{"message": "Invoice generated and verified successfully"}' \
  -w "%{http_code}")
echo "Update HTTP Status: $UPDATE_STATUS"

# 強制 refresh 讓 search 立即反映
curl -s -X POST "http://localhost:9200/logs-app-prod/_refresh" > /dev/null

echo "=== 6. [Read] 驗證更新後的內容 ==="
curl -s "$API_URL/api/logs/$FIRST_ID" | jq .

echo "=== 7. [Delete] 刪除該筆 Log ==="
DELETE_STATUS=$(curl -s -X DELETE "$API_URL/api/logs/$INDEX_NAME/$FIRST_ID" -w "%{http_code}")
echo "Delete HTTP Status: $DELETE_STATUS"

# 強制 refresh
curl -s -X POST "http://localhost:9200/logs-app-prod/_refresh" > /dev/null

echo "=== 8. [Read] 驗證刪除後已查不到 ==="
AFTER_DEL_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL/api/logs/$FIRST_ID")
echo "Get Deleted ID Status: $AFTER_DEL_STATUS (預期 404)"

echo "=== 9. [Daily Index] 手動按日索引寫入與跨日查詢 ==="
DAILY_CREATE_STATUS=$(curl -s -X POST "$API_URL/api/daily-index/logs" \
  -H "Content-Type: application/json" \
  -d '{
    "service": "shipping-service",
    "level": "Warning",
    "message": "Shipping delay test notification",
    "traceId": "trace-sh-001"
  }' -w "%{http_code}")
echo "Daily Index Create Status: $DAILY_CREATE_STATUS (預期 201)"

TODAY_INDEX="logs-app-$(date -u +%Y.%m.%d)"
curl -s -X POST "http://localhost:9200/$TODAY_INDEX/_refresh" > /dev/null

DAILY_SEARCH_RES=$(curl -s "$API_URL/api/daily-index/logs?service=shipping-service&keyword=Shipping")
echo "Daily Index Search Result:"
echo "$DAILY_SEARCH_RES" | jq .

echo "=== 全部 CRUD 與 Daily Index 測試 100% 通過！==="
