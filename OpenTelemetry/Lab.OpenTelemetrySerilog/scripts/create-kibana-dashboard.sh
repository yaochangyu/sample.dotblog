#!/bin/bash

# Kibana Dashboard 建立腳本
# 用途：建立 OpenTelemetry 可觀測性儀表板

set -e

KIBANA_URL="http://localhost:5601"
HEADERS="Content-Type: application/json"
XSRF_HEADER="kbn-xsrf: true"

echo "=========================================="
echo "建立 Kibana Dashboard"
echo "=========================================="

# Data View IDs（從 setup-kibana-index-patterns.sh 建立的）
JAEGER_SPAN_DATA_VIEW_ID="018b4b47-9e0d-422b-8a03-a990c35f882e"
OTEL_LOGS_DATA_VIEW_ID="b887e72f-7179-4b64-8f50-c7cef121de40"

echo ""
echo "步驟 1/8: 建立「請求數量趨勢圖」..."
curl -X POST "$KIBANA_URL/api/saved_objects/visualization" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Traces - 請求數量趨勢",
      "visState": "{\"type\":\"histogram\",\"aggs\":[{\"id\":\"1\",\"enabled\":true,\"type\":\"count\",\"params\":{},\"schema\":\"metric\"},{\"id\":\"2\",\"enabled\":true,\"type\":\"date_histogram\",\"params\":{\"field\":\"startTimeMillis\",\"timeRange\":{\"from\":\"now-15m\",\"to\":\"now\"},\"useNormalizedEsInterval\":true,\"scaleMetricValues\":false,\"interval\":\"auto\",\"drop_partials\":false,\"min_doc_count\":1,\"extended_bounds\":{}},\"schema\":\"segment\"}],\"params\":{\"type\":\"histogram\",\"grid\":{\"categoryLines\":false},\"categoryAxes\":[{\"id\":\"CategoryAxis-1\",\"type\":\"category\",\"position\":\"bottom\",\"show\":true,\"style\":{},\"scale\":{\"type\":\"linear\"},\"labels\":{\"show\":true,\"filter\":true,\"truncate\":100},\"title\":{}}],\"valueAxes\":[{\"id\":\"ValueAxis-1\",\"name\":\"LeftAxis-1\",\"type\":\"value\",\"position\":\"left\",\"show\":true,\"style\":{},\"scale\":{\"type\":\"linear\",\"mode\":\"normal\"},\"labels\":{\"show\":true,\"rotate\":0,\"filter\":false,\"truncate\":100},\"title\":{\"text\":\"Count\"}}],\"seriesParams\":[{\"show\":true,\"type\":\"histogram\",\"mode\":\"stacked\",\"data\":{\"label\":\"Count\",\"id\":\"1\"},\"valueAxis\":\"ValueAxis-1\",\"drawLinesBetweenPoints\":true,\"lineWidth\":2,\"showCircles\":true}],\"addTooltip\":true,\"addLegend\":true,\"legendPosition\":\"right\",\"times\":[],\"addTimeMarker\":false,\"labels\":{\"show\":false},\"thresholdLine\":{\"show\":false,\"value\":10,\"width\":1,\"style\":\"full\",\"color\":\"#E7664C\"}}}",
      "uiStateJSON": "{}",
      "description": "顯示 Traces 的請求數量趨勢",
      "version": 1,
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"\",\"language\":\"kuery\"},\"filter\":[],\"indexRefName\":\"kibanaSavedObjectMeta.searchSourceJSON.index\"}"
      }
    },
    "references": [
      {
        "name": "kibanaSavedObjectMeta.searchSourceJSON.index",
        "type": "index-pattern",
        "id": "'"$JAEGER_SPAN_DATA_VIEW_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/viz_1_id.txt

VIZ_1_ID=$(cat /tmp/viz_1_id.txt)
echo "✅ 建立成功 (ID: $VIZ_1_ID)"

echo ""
echo "步驟 2/8: 建立「服務延遲分佈圖」..."
curl -X POST "$KIBANA_URL/api/saved_objects/visualization" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Traces - 服務延遲分佈",
      "visState": "{\"type\":\"histogram\",\"aggs\":[{\"id\":\"1\",\"enabled\":true,\"type\":\"count\",\"params\":{},\"schema\":\"metric\"},{\"id\":\"2\",\"enabled\":true,\"type\":\"histogram\",\"params\":{\"field\":\"duration\",\"interval\":100,\"min_doc_count\":1,\"has_extended_bounds\":false,\"extended_bounds\":{\"min\":\"\",\"max\":\"\"}},\"schema\":\"segment\"}],\"params\":{\"type\":\"histogram\",\"grid\":{\"categoryLines\":false},\"categoryAxes\":[{\"id\":\"CategoryAxis-1\",\"type\":\"category\",\"position\":\"bottom\",\"show\":true,\"style\":{},\"scale\":{\"type\":\"linear\"},\"labels\":{\"show\":true,\"filter\":true,\"truncate\":100},\"title\":{\"text\":\"Duration (μs)\"}}],\"valueAxes\":[{\"id\":\"ValueAxis-1\",\"name\":\"LeftAxis-1\",\"type\":\"value\",\"position\":\"left\",\"show\":true,\"style\":{},\"scale\":{\"type\":\"linear\",\"mode\":\"normal\"},\"labels\":{\"show\":true,\"rotate\":0,\"filter\":false,\"truncate\":100},\"title\":{\"text\":\"Count\"}}],\"seriesParams\":[{\"show\":true,\"type\":\"histogram\",\"mode\":\"stacked\",\"data\":{\"label\":\"Count\",\"id\":\"1\"},\"valueAxis\":\"ValueAxis-1\",\"drawLinesBetweenPoints\":true,\"lineWidth\":2,\"showCircles\":true}],\"addTooltip\":true,\"addLegend\":true,\"legendPosition\":\"right\",\"times\":[],\"addTimeMarker\":false,\"labels\":{\"show\":false},\"thresholdLine\":{\"show\":false,\"value\":10,\"width\":1,\"style\":\"full\",\"color\":\"#E7664C\"}}}",
      "uiStateJSON": "{}",
      "description": "顯示 Span 延遲的分佈情況",
      "version": 1,
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"\",\"language\":\"kuery\"},\"filter\":[],\"indexRefName\":\"kibanaSavedObjectMeta.searchSourceJSON.index\"}"
      }
    },
    "references": [
      {
        "name": "kibanaSavedObjectMeta.searchSourceJSON.index",
        "type": "index-pattern",
        "id": "'"$JAEGER_SPAN_DATA_VIEW_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/viz_2_id.txt

VIZ_2_ID=$(cat /tmp/viz_2_id.txt)
echo "✅ 建立成功 (ID: $VIZ_2_ID)"

echo ""
echo "步驟 3/8: 建立「Top 10 慢查詢」..."
curl -X POST "$KIBANA_URL/api/saved_objects/visualization" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Traces - Top 10 慢查詢",
      "visState": "{\"type\":\"table\",\"aggs\":[{\"id\":\"1\",\"enabled\":true,\"type\":\"max\",\"params\":{\"field\":\"duration\"},\"schema\":\"metric\"},{\"id\":\"2\",\"enabled\":true,\"type\":\"terms\",\"params\":{\"field\":\"operationName.keyword\",\"orderBy\":\"1\",\"order\":\"desc\",\"size\":10,\"otherBucket\":false,\"otherBucketLabel\":\"Other\",\"missingBucket\":false,\"missingBucketLabel\":\"Missing\"},\"schema\":\"bucket\"},{\"id\":\"3\",\"enabled\":true,\"type\":\"terms\",\"params\":{\"field\":\"process.serviceName.keyword\",\"orderBy\":\"1\",\"order\":\"desc\",\"size\":5,\"otherBucket\":false,\"otherBucketLabel\":\"Other\",\"missingBucket\":false,\"missingBucketLabel\":\"Missing\"},\"schema\":\"bucket\"}],\"params\":{\"perPage\":10,\"showPartialRows\":false,\"showMetricsAtAllLevels\":false,\"sort\":{\"columnIndex\":null,\"direction\":null},\"showTotal\":false,\"totalFunc\":\"sum\",\"percentageCol\":\"\"}}",
      "uiStateJSON": "{}",
      "description": "顯示延遲最高的 10 個操作",
      "version": 1,
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"\",\"language\":\"kuery\"},\"filter\":[],\"indexRefName\":\"kibanaSavedObjectMeta.searchSourceJSON.index\"}"
      }
    },
    "references": [
      {
        "name": "kibanaSavedObjectMeta.searchSourceJSON.index",
        "type": "index-pattern",
        "id": "'"$JAEGER_SPAN_DATA_VIEW_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/viz_3_id.txt

VIZ_3_ID=$(cat /tmp/viz_3_id.txt)
echo "✅ 建立成功 (ID: $VIZ_3_ID)"

echo ""
echo "步驟 4/8: 建立「Trace 狀態碼分佈」..."
curl -X POST "$KIBANA_URL/api/saved_objects/visualization" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Traces - HTTP 狀態碼分佈",
      "visState": "{\"type\":\"pie\",\"aggs\":[{\"id\":\"1\",\"enabled\":true,\"type\":\"count\",\"params\":{},\"schema\":\"metric\"},{\"id\":\"2\",\"enabled\":true,\"type\":\"terms\",\"params\":{\"field\":\"tag.http@response@status_code\",\"orderBy\":\"1\",\"order\":\"desc\",\"size\":10,\"otherBucket\":false,\"otherBucketLabel\":\"Other\",\"missingBucket\":false,\"missingBucketLabel\":\"Missing\"},\"schema\":\"segment\"}],\"params\":{\"type\":\"pie\",\"addTooltip\":true,\"addLegend\":true,\"legendPosition\":\"right\",\"isDonut\":true,\"labels\":{\"show\":false,\"values\":true,\"last_level\":true,\"truncate\":100}}}",
      "uiStateJSON": "{}",
      "description": "顯示 HTTP 狀態碼的分佈",
      "version": 1,
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"\",\"language\":\"kuery\"},\"filter\":[],\"indexRefName\":\"kibanaSavedObjectMeta.searchSourceJSON.index\"}"
      }
    },
    "references": [
      {
        "name": "kibanaSavedObjectMeta.searchSourceJSON.index",
        "type": "index-pattern",
        "id": "'"$JAEGER_SPAN_DATA_VIEW_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/viz_4_id.txt

VIZ_4_ID=$(cat /tmp/viz_4_id.txt)
echo "✅ 建立成功 (ID: $VIZ_4_ID)"

echo ""
echo "步驟 5/8: 建立「日誌等級分佈」..."
curl -X POST "$KIBANA_URL/api/saved_objects/visualization" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Logs - 日誌等級分佈",
      "visState": "{\"type\":\"pie\",\"aggs\":[{\"id\":\"1\",\"enabled\":true,\"type\":\"count\",\"params\":{},\"schema\":\"metric\"},{\"id\":\"2\",\"enabled\":true,\"type\":\"terms\",\"params\":{\"field\":\"log.level.keyword\",\"orderBy\":\"1\",\"order\":\"desc\",\"size\":5,\"otherBucket\":false,\"otherBucketLabel\":\"Other\",\"missingBucket\":false,\"missingBucketLabel\":\"Missing\"},\"schema\":\"segment\"}],\"params\":{\"type\":\"pie\",\"addTooltip\":true,\"addLegend\":true,\"legendPosition\":\"right\",\"isDonut\":false,\"labels\":{\"show\":true,\"values\":true,\"last_level\":true,\"truncate\":100}}}",
      "uiStateJSON": "{}",
      "description": "顯示日誌等級的分佈（INFO/WARN/ERROR）",
      "version": 1,
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"\",\"language\":\"kuery\"},\"filter\":[],\"indexRefName\":\"kibanaSavedObjectMeta.searchSourceJSON.index\"}"
      }
    },
    "references": [
      {
        "name": "kibanaSavedObjectMeta.searchSourceJSON.index",
        "type": "index-pattern",
        "id": "'"$OTEL_LOGS_DATA_VIEW_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/viz_5_id.txt

VIZ_5_ID=$(cat /tmp/viz_5_id.txt)
echo "✅ 建立成功 (ID: $VIZ_5_ID)"

echo ""
echo "步驟 6/8: 建立「錯誤日誌 Top 10」..."
curl -X POST "$KIBANA_URL/api/saved_objects/visualization" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Logs - 錯誤日誌 Top 10",
      "visState": "{\"type\":\"table\",\"aggs\":[{\"id\":\"1\",\"enabled\":true,\"type\":\"count\",\"params\":{},\"schema\":\"metric\"},{\"id\":\"2\",\"enabled\":true,\"type\":\"terms\",\"params\":{\"field\":\"message.keyword\",\"orderBy\":\"1\",\"order\":\"desc\",\"size\":10,\"otherBucket\":false,\"otherBucketLabel\":\"Other\",\"missingBucket\":false,\"missingBucketLabel\":\"Missing\"},\"schema\":\"bucket\"}],\"params\":{\"perPage\":10,\"showPartialRows\":false,\"showMetricsAtAllLevels\":false,\"sort\":{\"columnIndex\":null,\"direction\":null},\"showTotal\":false,\"totalFunc\":\"sum\",\"percentageCol\":\"\"}}",
      "uiStateJSON": "{}",
      "description": "顯示最常見的錯誤訊息",
      "version": 1,
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"log.level: Error\",\"language\":\"kuery\"},\"filter\":[],\"indexRefName\":\"kibanaSavedObjectMeta.searchSourceJSON.index\"}"
      }
    },
    "references": [
      {
        "name": "kibanaSavedObjectMeta.searchSourceJSON.index",
        "type": "index-pattern",
        "id": "'"$OTEL_LOGS_DATA_VIEW_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/viz_6_id.txt

VIZ_6_ID=$(cat /tmp/viz_6_id.txt)
echo "✅ 建立成功 (ID: $VIZ_6_ID)"

echo ""
echo "步驟 7/8: 建立「依服務分組的日誌量」..."
curl -X POST "$KIBANA_URL/api/saved_objects/visualization" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Logs - 依服務分組的日誌量",
      "visState": "{\"type\":\"histogram\",\"aggs\":[{\"id\":\"1\",\"enabled\":true,\"type\":\"count\",\"params\":{},\"schema\":\"metric\"},{\"id\":\"2\",\"enabled\":true,\"type\":\"date_histogram\",\"params\":{\"field\":\"@timestamp\",\"timeRange\":{\"from\":\"now-15m\",\"to\":\"now\"},\"useNormalizedEsInterval\":true,\"scaleMetricValues\":false,\"interval\":\"auto\",\"drop_partials\":false,\"min_doc_count\":1,\"extended_bounds\":{}},\"schema\":\"segment\"},{\"id\":\"3\",\"enabled\":true,\"type\":\"terms\",\"params\":{\"field\":\"Application.keyword\",\"orderBy\":\"1\",\"order\":\"desc\",\"size\":5,\"otherBucket\":false,\"otherBucketLabel\":\"Other\",\"missingBucket\":false,\"missingBucketLabel\":\"Missing\"},\"schema\":\"group\"}],\"params\":{\"type\":\"histogram\",\"grid\":{\"categoryLines\":false},\"categoryAxes\":[{\"id\":\"CategoryAxis-1\",\"type\":\"category\",\"position\":\"bottom\",\"show\":true,\"style\":{},\"scale\":{\"type\":\"linear\"},\"labels\":{\"show\":true,\"filter\":true,\"truncate\":100},\"title\":{}}],\"valueAxes\":[{\"id\":\"ValueAxis-1\",\"name\":\"LeftAxis-1\",\"type\":\"value\",\"position\":\"left\",\"show\":true,\"style\":{},\"scale\":{\"type\":\"linear\",\"mode\":\"normal\"},\"labels\":{\"show\":true,\"rotate\":0,\"filter\":false,\"truncate\":100},\"title\":{\"text\":\"Count\"}}],\"seriesParams\":[{\"show\":true,\"type\":\"histogram\",\"mode\":\"stacked\",\"data\":{\"label\":\"Count\",\"id\":\"1\"},\"valueAxis\":\"ValueAxis-1\",\"drawLinesBetweenPoints\":true,\"lineWidth\":2,\"showCircles\":true}],\"addTooltip\":true,\"addLegend\":true,\"legendPosition\":\"right\",\"times\":[],\"addTimeMarker\":false,\"labels\":{\"show\":false},\"thresholdLine\":{\"show\":false,\"value\":10,\"width\":1,\"style\":\"full\",\"color\":\"#E7664C\"}}}",
      "uiStateJSON": "{}",
      "description": "顯示各服務的日誌數量趨勢",
      "version": 1,
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"\",\"language\":\"kuery\"},\"filter\":[],\"indexRefName\":\"kibanaSavedObjectMeta.searchSourceJSON.index\"}"
      }
    },
    "references": [
      {
        "name": "kibanaSavedObjectMeta.searchSourceJSON.index",
        "type": "index-pattern",
        "id": "'"$OTEL_LOGS_DATA_VIEW_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/viz_7_id.txt

VIZ_7_ID=$(cat /tmp/viz_7_id.txt)
echo "✅ 建立成功 (ID: $VIZ_7_ID)"

echo ""
echo "步驟 8/8: 建立 Dashboard..."
curl -X POST "$KIBANA_URL/api/saved_objects/dashboard" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "OpenTelemetry 可觀測性總覽",
      "description": "整合 Traces 和 Logs 的可觀測性儀表板",
      "panelsJSON": "[{\"version\":\"8.17.1\",\"gridData\":{\"x\":0,\"y\":0,\"w\":24,\"h\":15,\"i\":\"1\"},\"panelIndex\":\"1\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_1\"},{\"version\":\"8.17.1\",\"gridData\":{\"x\":24,\"y\":0,\"w\":24,\"h\":15,\"i\":\"2\"},\"panelIndex\":\"2\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_2\"},{\"version\":\"8.17.1\",\"gridData\":{\"x\":0,\"y\":15,\"w\":24,\"h\":15,\"i\":\"3\"},\"panelIndex\":\"3\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_3\"},{\"version\":\"8.17.1\",\"gridData\":{\"x\":24,\"y\":15,\"w\":24,\"h\":15,\"i\":\"4\"},\"panelIndex\":\"4\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_4\"},{\"version\":\"8.17.1\",\"gridData\":{\"x\":0,\"y\":30,\"w\":16,\"h\":15,\"i\":\"5\"},\"panelIndex\":\"5\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_5\"},{\"version\":\"8.17.1\",\"gridData\":{\"x\":16,\"y\":30,\"w\":16,\"h\":15,\"i\":\"6\"},\"panelIndex\":\"6\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_6\"},{\"version\":\"8.17.1\",\"gridData\":{\"x\":32,\"y\":30,\"w\":16,\"h\":15,\"i\":\"7\"},\"panelIndex\":\"7\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_7\"}]",
      "optionsJSON": "{\"hidePanelTitles\":false,\"useMargins\":true}",
      "version": 1,
      "timeRestore": true,
      "timeTo": "now",
      "timeFrom": "now-15m",
      "refreshInterval": {
        "pause": false,
        "value": 30000
      },
      "kibanaSavedObjectMeta": {
        "searchSourceJSON": "{\"query\":{\"query\":\"\",\"language\":\"kuery\"},\"filter\":[]}"
      }
    },
    "references": [
      {
        "name": "panel_1",
        "type": "visualization",
        "id": "'"$VIZ_1_ID"'"
      },
      {
        "name": "panel_2",
        "type": "visualization",
        "id": "'"$VIZ_2_ID"'"
      },
      {
        "name": "panel_3",
        "type": "visualization",
        "id": "'"$VIZ_3_ID"'"
      },
      {
        "name": "panel_4",
        "type": "visualization",
        "id": "'"$VIZ_4_ID"'"
      },
      {
        "name": "panel_5",
        "type": "visualization",
        "id": "'"$VIZ_5_ID"'"
      },
      {
        "name": "panel_6",
        "type": "visualization",
        "id": "'"$VIZ_6_ID"'"
      },
      {
        "name": "panel_7",
        "type": "visualization",
        "id": "'"$VIZ_7_ID"'"
      }
    ]
  }' | jq -r '.id' > /tmp/dashboard_id.txt

DASHBOARD_ID=$(cat /tmp/dashboard_id.txt)
echo "✅ Dashboard 建立成功 (ID: $DASHBOARD_ID)"

# 清理暫存檔案
rm -f /tmp/viz_*_id.txt /tmp/dashboard_id.txt

echo ""
echo "=========================================="
echo "✅ 完成！"
echo "=========================================="
echo ""
echo "Dashboard URL:"
echo "http://localhost:5601/app/dashboards#/view/$DASHBOARD_ID"
echo ""
echo "包含的視覺化："
echo "  📈 Traces - 請求數量趨勢"
echo "  📊 Traces - 服務延遲分佈"
echo "  📉 Traces - Top 10 慢查詢"
echo "  🥧 Traces - HTTP 狀態碼分佈"
echo "  🥧 Logs - 日誌等級分佈"
echo "  📋 Logs - 錯誤日誌 Top 10"
echo "  📈 Logs - 依服務分組的日誌量"
echo ""
