#!/bin/bash

set -e

KIBANA_URL="${KIBANA_URL:-http://localhost:5601}"

echo "=========================================="
echo "建立 Kibana Saved Searches"
echo "Kibana URL: $KIBANA_URL"
echo "=========================================="
echo ""

# 等待 Kibana 準備就緒
echo "等待 Kibana 服務準備就緒..."
until curl -s "$KIBANA_URL/api/status" | grep -q '"level":"available"'; do
  echo "  Kibana 尚未準備就緒，等待 5 秒..."
  sleep 5
done
echo "✅ Kibana 已準備就緒"
echo ""

# 取得 Data View IDs
echo "取得 Data View IDs..."
LOGS_DV_ID=$(curl -s "$KIBANA_URL/api/data_views" -H "kbn-xsrf: true" | jq -r '.data_view[] | select(.title == "otel-logs-*") | .id')
TRACES_DV_ID=$(curl -s "$KIBANA_URL/api/data_views" -H "kbn-xsrf: true" | jq -r '.data_view[] | select(.title == "otel-traces-*") | .id')

echo "  Logs Data View ID: $LOGS_DV_ID"
echo "  Traces Data View ID: $TRACES_DV_ID"
echo ""

# 1. 建立「錯誤日誌查詢」Saved Search
echo "[1/3] 建立 Saved Search: 錯誤日誌查詢..."
curl -X POST "$KIBANA_URL/api/saved_objects/search" \
  -H "kbn-xsrf: true" \
  -H "Content-Type: application/json" \
  -d "{
    \"attributes\": {
      \"title\": \"[OTel] 錯誤日誌 (ERROR)\",
      \"description\": \"顯示所有 ERROR 等級的日誌\",
      \"columns\": [\"@timestamp\", \"service.name\", \"body\", \"severity_text\"],
      \"sort\": [[\"@timestamp\", \"desc\"]],
      \"kibanaSavedObjectMeta\": {
        \"searchSourceJSON\": \"{\\\"query\\\":{\\\"query\\\":\\\"severity_text: ERROR\\\",\\\"language\\\":\\\"kuery\\\"},\\\"filter\\\":[],\\\"indexRefName\\\":\\\"kibanaSavedObjectMeta.searchSourceJSON.index\\\"}\"
      }
    },
    \"references\": [
      {
        \"id\": \"$LOGS_DV_ID\",
        \"name\": \"kibanaSavedObjectMeta.searchSourceJSON.index\",
        \"type\": \"index-pattern\"
      }
    ]
  }" 2>/dev/null || echo "  (Saved Search 可能已存在)"
echo ""
echo "✅ 錯誤日誌查詢建立完成"
echo ""

# 2. 建立「特定服務日誌查詢」Saved Search
echo "[2/3] 建立 Saved Search: Backend-A 日誌..."
curl -X POST "$KIBANA_URL/api/saved_objects/search" \
  -H "kbn-xsrf: true" \
  -H "Content-Type: application/json" \
  -d "{
    \"attributes\": {
      \"title\": \"[OTel] Backend-A 日誌\",
      \"description\": \"顯示 backend-a 服務的所有日誌\",
      \"columns\": [\"@timestamp\", \"severity_text\", \"body\"],
      \"sort\": [[\"@timestamp\", \"desc\"]],
      \"kibanaSavedObjectMeta\": {
        \"searchSourceJSON\": \"{\\\"query\\\":{\\\"query\\\":\\\"service.name: backend-a\\\",\\\"language\\\":\\\"kuery\\\"},\\\"filter\\\":[],\\\"indexRefName\\\":\\\"kibanaSavedObjectMeta.searchSourceJSON.index\\\"}\"
      }
    },
    \"references\": [
      {
        \"id\": \"$LOGS_DV_ID\",
        \"name\": \"kibanaSavedObjectMeta.searchSourceJSON.index\",
        \"type\": \"index-pattern\"
      }
    ]
  }" 2>/dev/null || echo "  (Saved Search 可能已存在)"
echo ""
echo "✅ Backend-A 日誌查詢建立完成"
echo ""

# 3. 建立「慢請求查詢」Saved Search（Traces）
echo "[3/3] 建立 Saved Search: 慢請求 Traces..."
curl -X POST "$KIBANA_URL/api/saved_objects/search" \
  -H "kbn-xsrf: true" \
  -H "Content-Type: application/json" \
  -d "{
    \"attributes\": {
      \"title\": \"[OTel] 慢請求 Traces (>1s)\",
      \"description\": \"顯示執行時間超過 1 秒的 traces\",
      \"columns\": [\"@timestamp\", \"service.name\", \"span.name\", \"span.duration\"],
      \"sort\": [[\"span.duration\", \"desc\"]],
      \"kibanaSavedObjectMeta\": {
        \"searchSourceJSON\": \"{\\\"query\\\":{\\\"query\\\":\\\"span.duration > 1000000\\\",\\\"language\\\":\\\"kuery\\\"},\\\"filter\\\":[],\\\"indexRefName\\\":\\\"kibanaSavedObjectMeta.searchSourceJSON.index\\\"}\"
      }
    },
    \"references\": [
      {
        \"id\": \"$TRACES_DV_ID\",
        \"name\": \"kibanaSavedObjectMeta.searchSourceJSON.index\",
        \"type\": \"index-pattern\"
      }
    ]
  }" 2>/dev/null || echo "  (Saved Search 可能已存在)"
echo ""
echo "✅ 慢請求查詢建立完成"
echo ""

echo "=========================================="
echo "✅ 所有 Saved Searches 建立完成！"
echo "=========================================="
echo ""
echo "您可以在 Kibana Discover 頁面的「已儲存的搜尋」中找到這些查詢："
echo "  - [OTel] 錯誤日誌 (ERROR)"
echo "  - [OTel] Backend-A 日誌"
echo "  - [OTel] 慢請求 Traces (>1s)"
echo ""
echo "訪問 Kibana："
echo "  http://localhost:5601/app/discover"
echo ""
