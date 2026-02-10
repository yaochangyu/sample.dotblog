#!/bin/bash

set -e

ELASTICSEARCH_URL="${ELASTICSEARCH_URL:-http://localhost:9200}"
JAEGER_URL="${JAEGER_URL:-http://localhost:16686}"
KIBANA_URL="${KIBANA_URL:-http://localhost:5601}"
SEQ_URL="${SEQ_URL:-http://localhost:5341}"
ASPIRE_URL="${ASPIRE_URL:-http://localhost:18888}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost:3000}"

echo "=========================================="
echo "OpenTelemetry + Elasticsearch 整合驗證"
echo "=========================================="
echo ""

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

pass() {
  echo -e "${GREEN}✅ PASS${NC} - $1"
}

fail() {
  echo -e "${RED}❌ FAIL${NC} - $1"
  FAILED=1
}

warn() {
  echo -e "${YELLOW}⚠️  WARN${NC} - $1"
}

FAILED=0

# 1. 檢查服務健康狀態
echo "[1/7] 檢查服務健康狀態..."
echo ""

# Elasticsearch
if curl -s "$ELASTICSEARCH_URL/_cluster/health" | grep -q '"status":"green\|yellow"'; then
  pass "Elasticsearch 運作正常 ($ELASTICSEARCH_URL)"
else
  fail "Elasticsearch 無法訪問或狀態異常"
fi

# Kibana
if curl -s "$KIBANA_URL/api/status" | grep -q '"level":"available"'; then
  pass "Kibana 運作正常 ($KIBANA_URL)"
else
  fail "Kibana 無法訪問或狀態異常"
fi

# Jaeger
if curl -s "$JAEGER_URL/api/services" | grep -q '"data"'; then
  pass "Jaeger UI 運作正常 ($JAEGER_URL)"
else
  fail "Jaeger UI 無法訪問"
fi

# Seq
if curl -s "$SEQ_URL" | grep -q 'Seq'; then
  pass "Seq 運作正常 ($SEQ_URL)"
else
  warn "Seq 無法訪問（非必要）"
fi

# Aspire Dashboard
if curl -s "$ASPIRE_URL" > /dev/null 2>&1; then
  pass "Aspire Dashboard 運作正常 ($ASPIRE_URL)"
else
  warn "Aspire Dashboard 無法訪問（非必要）"
fi

echo ""

# 2. 發送測試請求
echo "[2/7] 發送測試請求..."
echo ""

TEST_START_TIME=$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")
echo "  測試開始時間: $TEST_START_TIME"

for i in {1..3}; do
  echo "  發送請求 #$i..."
  if curl -s "$FRONTEND_URL/api/weather" > /dev/null; then
    echo "    ✓ 請求成功"
  else
    fail "測試請求失敗"
  fi
  sleep 1
done

echo "  等待 5 秒讓資料寫入 Elasticsearch..."
sleep 5
echo ""

# 3. 檢查 Elasticsearch 索引
echo "[3/7] 檢查 Elasticsearch 索引..."
echo ""

INDICES=$(curl -s "$ELASTICSEARCH_URL/_cat/indices?v" | grep -E "(otel-|jaeger-)")
if echo "$INDICES" | grep -q "otel-traces"; then
  pass "otel-traces 索引存在"
else
  fail "otel-traces 索引不存在"
fi

if echo "$INDICES" | grep -q "otel-logs"; then
  pass "otel-logs 索引存在"
else
  fail "otel-logs 索引不存在"
fi

if echo "$INDICES" | grep -q "otel-metrics"; then
  pass "otel-metrics 索引存在"
else
  warn "otel-metrics 索引不存在（可能尚未產生）"
fi

if echo "$INDICES" | grep -q "jaeger-jaeger-span"; then
  pass "jaeger-span 索引存在"
else
  warn "jaeger-span 索引不存在（可能尚未產生）"
fi

echo ""

# 4. 驗證 Elasticsearch 資料內容
echo "[4/7] 驗證 Elasticsearch 資料內容..."
echo ""

# 檢查 traces
TRACES_COUNT=$(curl -s "$ELASTICSEARCH_URL/otel-traces/_count" | jq -r '.count')
if [ "$TRACES_COUNT" -gt 0 ]; then
  pass "otel-traces 有資料（$TRACES_COUNT 筆）"

  # 檢查 trace 資料結構
  SAMPLE_TRACE=$(curl -s "$ELASTICSEARCH_URL/otel-traces/_search?size=1" | jq -r '.hits.hits[0]._source')
  if echo "$SAMPLE_TRACE" | jq -e '.trace.id' > /dev/null; then
    pass "  ├─ trace.id 欄位存在"
  else
    fail "  ├─ trace.id 欄位缺失"
  fi

  if echo "$SAMPLE_TRACE" | jq -e '.span.name' > /dev/null; then
    pass "  ├─ span.name 欄位存在"
  else
    fail "  ├─ span.name 欄位缺失"
  fi

  if echo "$SAMPLE_TRACE" | jq -e '.service.name' > /dev/null; then
    pass "  └─ service.name 欄位存在"
  else
    fail "  └─ service.name 欄位缺失"
  fi
else
  fail "otel-traces 無資料"
fi

# 檢查 logs
LOGS_COUNT=$(curl -s "$ELASTICSEARCH_URL/otel-logs/_count" | jq -r '.count')
if [ "$LOGS_COUNT" -gt 0 ]; then
  pass "otel-logs 有資料（$LOGS_COUNT 筆）"
else
  fail "otel-logs 無資料"
fi

echo ""

# 5. 驗證 Jaeger UI 查詢
echo "[5/7] 驗證 Jaeger UI 可查詢資料..."
echo ""

SERVICES=$(curl -s "$JAEGER_URL/api/services" | jq -r '.data[]')
if echo "$SERVICES" | grep -q "backend-a"; then
  pass "Jaeger 可查詢到 backend-a 服務"
else
  fail "Jaeger 無法查詢到 backend-a 服務"
fi

if echo "$SERVICES" | grep -q "backend-b"; then
  pass "Jaeger 可查詢到 backend-b 服務"
else
  fail "Jaeger 無法查詢到 backend-b 服務"
fi

# 查詢最近的 traces
TRACES=$(curl -s "$JAEGER_URL/api/traces?service=backend-a&limit=1")
TRACE_COUNT=$(echo "$TRACES" | jq -r '.data | length')
if [ "$TRACE_COUNT" -gt 0 ]; then
  SPAN_COUNT=$(echo "$TRACES" | jq -r '.data[0].spans | length')
  pass "Jaeger 可查詢到完整 trace（$SPAN_COUNT 個 spans）"
else
  fail "Jaeger 無法查詢到 traces"
fi

echo ""

# 6. 驗證 Kibana Data Views
echo "[6/7] 驗證 Kibana Data Views..."
echo ""

DATA_VIEWS=$(curl -s "$KIBANA_URL/api/data_views" -H "kbn-xsrf: true" | jq -r '.data_view[].title')
if echo "$DATA_VIEWS" | grep -q "otel-traces"; then
  pass "Kibana otel-traces Data View 存在"
else
  fail "Kibana otel-traces Data View 不存在"
fi

if echo "$DATA_VIEWS" | grep -q "otel-logs"; then
  pass "Kibana otel-logs Data View 存在"
else
  fail "Kibana otel-logs Data View 不存在"
fi

if echo "$DATA_VIEWS" | grep -q "otel-metrics"; then
  pass "Kibana otel-metrics Data View 存在"
else
  fail "Kibana otel-metrics Data View 不存在"
fi

echo ""

# 7. 驗證 ILM 策略
echo "[7/7] 驗證 ILM 策略..."
echo ""

if curl -s "$ELASTICSEARCH_URL/_ilm/policy/otel-14day-policy" | grep -q '"otel-14day-policy"'; then
  pass "ILM 策略 otel-14day-policy 存在"

  # 檢查策略是否被使用
  POLICY_USAGE=$(curl -s "$ELASTICSEARCH_URL/_ilm/policy/otel-14day-policy" | jq -r '."otel-14day-policy"."in_use_by"."composable_templates" | length')
  if [ "$POLICY_USAGE" -gt 0 ]; then
    pass "  └─ ILM 策略已套用到 $POLICY_USAGE 個索引模板"
  else
    warn "  └─ ILM 策略尚未套用到任何索引模板"
  fi
else
  fail "ILM 策略 otel-14day-policy 不存在"
fi

echo ""

# 總結
echo "=========================================="
if [ $FAILED -eq 0 ]; then
  echo -e "${GREEN}✅ 所有驗證通過！${NC}"
  echo "=========================================="
  echo ""
  echo "您可以透過以下網址訪問各服務："
  echo "  - Kibana:           $KIBANA_URL"
  echo "  - Jaeger UI:        $JAEGER_URL"
  echo "  - Seq:              $SEQ_URL"
  echo "  - Aspire Dashboard: $ASPIRE_URL"
  echo "  - Elasticsearch:    $ELASTICSEARCH_URL"
  echo ""
  exit 0
else
  echo -e "${RED}❌ 部分驗證失敗，請檢查錯誤訊息${NC}"
  echo "=========================================="
  exit 1
fi
