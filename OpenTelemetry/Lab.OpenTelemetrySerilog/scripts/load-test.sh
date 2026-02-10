#!/bin/bash

set -e

FRONTEND_URL="${FRONTEND_URL:-http://localhost:3000}"
ELASTICSEARCH_URL="${ELASTICSEARCH_URL:-http://localhost:9200}"

# 預設參數
REQUESTS=${REQUESTS:-1000}
CONCURRENCY=${CONCURRENCY:-10}

echo "=========================================="
echo "OpenTelemetry 壓力測試"
echo "=========================================="
echo ""
echo "測試目標: $FRONTEND_URL/api/weather"
echo "請求數量: $REQUESTS"
echo "併發數:   $CONCURRENCY"
echo ""

# 檢查是否安裝 ab（Apache Bench）
if ! command -v ab &> /dev/null; then
  echo "錯誤：未安裝 Apache Bench (ab)"
  echo "請安裝："
  echo "  - Ubuntu/Debian: sudo apt-get install apache2-utils"
  echo "  - macOS:         brew install httpd"
  echo "  - Windows (WSL): sudo apt-get install apache2-utils"
  exit 1
fi

# 記錄開始狀態
echo "[1/3] 記錄測試前的索引狀態..."
echo ""

TRACES_BEFORE=$(curl -s "$ELASTICSEARCH_URL/otel-traces/_count" | jq -r '.count')
LOGS_BEFORE=$(curl -s "$ELASTICSEARCH_URL/otel-logs/_count" | jq -r '.count')
METRICS_BEFORE=$(curl -s "$ELASTICSEARCH_URL/otel-metrics/_count" 2>/dev/null | jq -r '.count' || echo "0")

echo "  Traces:  $TRACES_BEFORE 筆"
echo "  Logs:    $LOGS_BEFORE 筆"
echo "  Metrics: $METRICS_BEFORE 筆"
echo ""

# 執行壓力測試
echo "[2/3] 開始壓力測試..."
echo ""

START_TIME=$(date +%s)

ab -n "$REQUESTS" -c "$CONCURRENCY" "$FRONTEND_URL/api/weather" 2>&1 | tee /tmp/load-test-result.txt

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
echo "測試完成！耗時: ${DURATION} 秒"
echo ""

# 等待資料寫入
echo "等待 10 秒讓資料完全寫入 Elasticsearch..."
sleep 10
echo ""

# 記錄結束狀態
echo "[3/3] 檢查測試後的索引狀態..."
echo ""

TRACES_AFTER=$(curl -s "$ELASTICSEARCH_URL/otel-traces/_count" | jq -r '.count')
LOGS_AFTER=$(curl -s "$ELASTICSEARCH_URL/otel-logs/_count" | jq -r '.count')
METRICS_AFTER=$(curl -s "$ELASTICSEARCH_URL/otel-metrics/_count" 2>/dev/null | jq -r '.count' || echo "0")

TRACES_GROWTH=$((TRACES_AFTER - TRACES_BEFORE))
LOGS_GROWTH=$((LOGS_AFTER - LOGS_BEFORE))
METRICS_GROWTH=$((METRICS_AFTER - METRICS_BEFORE))

echo "  Traces:  $TRACES_AFTER 筆 (+$TRACES_GROWTH)"
echo "  Logs:    $LOGS_AFTER 筆 (+$LOGS_GROWTH)"
echo "  Metrics: $METRICS_AFTER 筆 (+$METRICS_GROWTH)"
echo ""

# 檢查索引大小
echo "Elasticsearch 索引大小："
curl -s "$ELASTICSEARCH_URL/_cat/indices?v&h=index,store.size" | grep -E "otel-|jaeger-"
echo ""

# 顯示測試總結
echo "=========================================="
echo "壓力測試總結"
echo "=========================================="
echo ""

# 從 ab 結果中提取關鍵指標
if [ -f /tmp/load-test-result.txt ]; then
  REQUESTS_PER_SEC=$(grep "Requests per second" /tmp/load-test-result.txt | awk '{print $4}')
  TIME_PER_REQUEST=$(grep "Time per request.*mean\)" /tmp/load-test-result.txt | awk '{print $4}')
  FAILED_REQUESTS=$(grep "Failed requests" /tmp/load-test-result.txt | awk '{print $3}')

  echo "效能指標："
  echo "  - 每秒請求數:      $REQUESTS_PER_SEC req/s"
  echo "  - 平均回應時間:    $TIME_PER_REQUEST ms"
  echo "  - 失敗請求:        $FAILED_REQUESTS"
  echo ""
fi

echo "資料增長："
echo "  - Traces 增加:     $TRACES_GROWTH 筆"
echo "  - Logs 增加:       $LOGS_GROWTH 筆"
echo "  - Metrics 增加:    $METRICS_GROWTH 筆"
echo ""

echo "=========================================="
echo "✅ 壓力測試完成！"
echo "=========================================="
echo ""
echo "建議檢查項目："
echo "  1. OTel Collector CPU/Memory 使用率"
echo "  2. Elasticsearch Heap 使用率"
echo "  3. Jaeger UI 查詢效能"
echo "  4. Kibana Discover 查詢效能"
echo ""
