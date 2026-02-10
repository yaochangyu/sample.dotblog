#!/bin/bash

set -e

KIBANA_URL="${KIBANA_URL:-http://localhost:5601}"

echo "=========================================="
echo "建立 Kibana Index Patterns (Data Views)"
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

# 1. 建立 otel-traces-* Data View
echo "[1/4] 建立 otel-traces-* Data View..."
curl -X POST "$KIBANA_URL/api/data_views/data_view" \
  -H "kbn-xsrf: true" \
  -H "Content-Type: application/json" \
  -d '{
    "data_view": {
      "title": "otel-traces-*",
      "name": "OpenTelemetry Traces",
      "timeFieldName": "@timestamp"
    }
  }' 2>/dev/null || echo "  (Data View 可能已存在)"
echo ""
echo "✅ otel-traces Data View 處理完成"
echo ""

# 2. 建立 otel-logs-* Data View
echo "[2/4] 建立 otel-logs-* Data View..."
curl -X POST "$KIBANA_URL/api/data_views/data_view" \
  -H "kbn-xsrf: true" \
  -H "Content-Type: application/json" \
  -d '{
    "data_view": {
      "title": "otel-logs-*",
      "name": "OpenTelemetry Logs",
      "timeFieldName": "@timestamp"
    }
  }' 2>/dev/null || echo "  (Data View 可能已存在)"
echo ""
echo "✅ otel-logs Data View 處理完成"
echo ""

# 3. 建立 otel-metrics-* Data View
echo "[3/4] 建立 otel-metrics-* Data View..."
curl -X POST "$KIBANA_URL/api/data_views/data_view" \
  -H "kbn-xsrf: true" \
  -H "Content-Type: application/json" \
  -d '{
    "data_view": {
      "title": "otel-metrics-*",
      "name": "OpenTelemetry Metrics",
      "timeFieldName": "@timestamp"
    }
  }' 2>/dev/null || echo "  (Data View 可能已存在)"
echo ""
echo "✅ otel-metrics Data View 處理完成"
echo ""

# 4. 建立 jaeger-span-* Data View
echo "[4/4] 建立 jaeger-span-* Data View..."
curl -X POST "$KIBANA_URL/api/data_views/data_view" \
  -H "kbn-xsrf: true" \
  -H "Content-Type: application/json" \
  -d '{
    "data_view": {
      "title": "jaeger-jaeger-span-*",
      "name": "Jaeger Spans",
      "timeFieldName": "startTime"
    }
  }' 2>/dev/null || echo "  (Data View 可能已存在)"
echo ""
echo "✅ jaeger-span Data View 處理完成"
echo ""

# 5. 列出所有 Data Views
echo "=========================================="
echo "驗證 Data Views 建立結果"
echo "=========================================="
echo ""
curl -s "$KIBANA_URL/api/data_views" -H "kbn-xsrf: true" | jq -r '.data_view[] | "  - \(.name) (\(.title))"' 2>/dev/null || echo "無法列出 Data Views（需要 jq 工具）"
echo ""

echo "=========================================="
echo "✅ 所有 Data Views 建立完成！"
echo "=========================================="
echo ""
echo "您可以透過以下網址訪問 Kibana："
echo "  http://localhost:5601"
echo ""
