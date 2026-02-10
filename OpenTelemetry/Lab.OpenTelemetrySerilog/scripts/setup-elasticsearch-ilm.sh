#!/bin/bash

set -e

ELASTICSEARCH_URL="${ELASTICSEARCH_URL:-http://localhost:9200}"

echo "=========================================="
echo "設定 Elasticsearch ILM 策略"
echo "Elasticsearch URL: $ELASTICSEARCH_URL"
echo "=========================================="
echo ""

# 1. 建立 ILM 策略：14 天後刪除
echo "[1/4] 建立 ILM 策略 (otel-14day-policy)..."
curl -X PUT "$ELASTICSEARCH_URL/_ilm/policy/otel-14day-policy" \
  -H 'Content-Type: application/json' \
  -d '{
    "policy": {
      "phases": {
        "hot": {
          "actions": {
            "rollover": {
              "max_age": "1d",
              "max_size": "50gb"
            }
          }
        },
        "delete": {
          "min_age": "14d",
          "actions": {
            "delete": {}
          }
        }
      }
    }
  }'
echo ""
echo "✅ ILM 策略建立完成"
echo ""

# 2. 套用到 OTel Logs 索引
echo "[2/4] 套用 ILM 策略到 otel-logs..."
curl -X PUT "$ELASTICSEARCH_URL/_index_template/otel-logs-template" \
  -H 'Content-Type: application/json' \
  -d '{
    "index_patterns": ["otel-logs-*"],
    "template": {
      "settings": {
        "index.lifecycle.name": "otel-14day-policy",
        "index.lifecycle.rollover_alias": "otel-logs"
      }
    }
  }'
echo ""
echo "✅ otel-logs 模板設定完成"
echo ""

# 3. 套用到 OTel Traces 索引
echo "[3/4] 套用 ILM 策略到 otel-traces..."
curl -X PUT "$ELASTICSEARCH_URL/_index_template/otel-traces-template" \
  -H 'Content-Type: application/json' \
  -d '{
    "index_patterns": ["otel-traces-*"],
    "template": {
      "settings": {
        "index.lifecycle.name": "otel-14day-policy",
        "index.lifecycle.rollover_alias": "otel-traces"
      }
    }
  }'
echo ""
echo "✅ otel-traces 模板設定完成"
echo ""

# 4. 套用到 OTel Metrics 索引
echo "[4/4] 套用 ILM 策略到 otel-metrics..."
curl -X PUT "$ELASTICSEARCH_URL/_index_template/otel-metrics-template" \
  -H 'Content-Type: application/json' \
  -d '{
    "index_patterns": ["otel-metrics-*"],
    "template": {
      "settings": {
        "index.lifecycle.name": "otel-14day-policy",
        "index.lifecycle.rollover_alias": "otel-metrics"
      }
    }
  }'
echo ""
echo "✅ otel-metrics 模板設定完成"
echo ""

# 5. 驗證 ILM 策略
echo "=========================================="
echo "驗證 ILM 策略設定"
echo "=========================================="
echo ""
echo "查詢 ILM 策略："
curl -s "$ELASTICSEARCH_URL/_ilm/policy/otel-14day-policy" | jq '.'
echo ""

echo "=========================================="
echo "✅ ILM 策略設定完成！"
echo "=========================================="
echo ""
echo "注意事項："
echo "1. 現有索引（otel-logs, otel-traces, otel-metrics）不會自動套用 ILM"
echo "2. 新建立的索引會自動套用 14 天刪除策略"
echo "3. 若要對現有索引套用策略，需要手動執行 rollover"
echo ""
