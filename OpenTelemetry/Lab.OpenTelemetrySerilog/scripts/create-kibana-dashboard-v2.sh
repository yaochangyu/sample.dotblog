#!/bin/bash

# Kibana Dashboard 建立腳本 v2
# 使用更簡化的方式建立視覺化

set -e

KIBANA_URL="http://localhost:5601"
HEADERS="Content-Type: application/json"
XSRF_HEADER="kbn-xsrf: true"

echo "=========================================="
echo "清理舊的 Dashboard 和 Visualizations"
echo "=========================================="

# 刪除舊的 dashboard
OLD_DASHBOARD_ID="5d135a8e-a5a3-438e-9780-c4d69840a655"
curl -X DELETE "$KIBANA_URL/api/saved_objects/dashboard/$OLD_DASHBOARD_ID" \
  -H "$XSRF_HEADER" 2>/dev/null || echo "舊 dashboard 不存在或已刪除"

# 刪除舊的 visualizations
for viz_id in f7caa774-6fcd-460b-a30e-d7e6d881919c a6ffa0a8-4ec3-4079-ab38-e70de9549294 \
              9a551d1d-c214-4e26-ad1a-03c42d34efc8 0c45bc56-e95f-4dfc-abbd-8bca17a105c3 \
              8fc88c08-584e-4825-947d-49dd09166062 c38c5bfa-334b-4c45-afc9-5e2fd7800c4f \
              381fb0ed-6666-4519-93f8-f19882e11619; do
  curl -X DELETE "$KIBANA_URL/api/saved_objects/visualization/$viz_id" \
    -H "$XSRF_HEADER" 2>/dev/null || true
done

echo ""
echo "=========================================="
echo "建立新的 Dashboard（使用 Lens）"
echo "=========================================="

# Data View IDs
JAEGER_SPAN_DATA_VIEW_ID="018b4b47-9e0d-422b-8a03-a990c35f882e"
OTEL_LOGS_DATA_VIEW_ID="b887e72f-7179-4b64-8f50-c7cef121de40"

echo ""
echo "步驟 1/5: 建立「請求數量趨勢」（Lens）..."
VIZ_1_RESPONSE=$(curl -X POST "$KIBANA_URL/api/saved_objects/lens" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Traces - 請求數量趨勢",
      "description": "顯示 Traces 的請求數量趨勢",
      "visualizationType": "lnsXY",
      "state": {
        "datasourceStates": {
          "formBased": {
            "layers": {
              "layer1": {
                "columns": {
                  "col1": {
                    "label": "@timestamp",
                    "dataType": "date",
                    "operationType": "date_histogram",
                    "sourceField": "startTimeMillis",
                    "isBucketed": true,
                    "scale": "interval",
                    "params": {
                      "interval": "auto"
                    }
                  },
                  "col2": {
                    "label": "Count",
                    "dataType": "number",
                    "operationType": "count",
                    "isBucketed": false,
                    "scale": "ratio",
                    "sourceField": "___records___"
                  }
                },
                "columnOrder": ["col1", "col2"],
                "incompleteColumns": {}
              }
            }
          }
        },
        "visualization": {
          "legend": {
            "isVisible": true,
            "position": "right"
          },
          "valueLabels": "hide",
          "fittingFunction": "None",
          "axisTitlesVisibilitySettings": {
            "x": true,
            "yLeft": true,
            "yRight": true
          },
          "tickLabelsVisibilitySettings": {
            "x": true,
            "yLeft": true,
            "yRight": true
          },
          "gridlinesVisibilitySettings": {
            "x": true,
            "yLeft": true,
            "yRight": true
          },
          "preferredSeriesType": "bar_stacked",
          "layers": [
            {
              "layerId": "layer1",
              "accessors": ["col2"],
              "position": "top",
              "seriesType": "bar_stacked",
              "showGridlines": false,
              "layerType": "data",
              "xAccessor": "col1"
            }
          ]
        },
        "query": {
          "query": "",
          "language": "kuery"
        },
        "filters": []
      },
      "references": [
        {
          "type": "index-pattern",
          "id": "'"$JAEGER_SPAN_DATA_VIEW_ID"'",
          "name": "indexpattern-datasource-layer-layer1"
        }
      ]
    }
  }')

VIZ_1_ID=$(echo $VIZ_1_RESPONSE | jq -r '.id')
echo "✅ 建立成功 (ID: $VIZ_1_ID)"

echo ""
echo "步驟 2/5: 建立「服務延遲分佈」（Lens）..."
VIZ_2_RESPONSE=$(curl -X POST "$KIBANA_URL/api/saved_objects/lens" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Traces - 服務延遲分佈",
      "description": "顯示 Span 延遲的分佈情況",
      "visualizationType": "lnsXY",
      "state": {
        "datasourceStates": {
          "formBased": {
            "layers": {
              "layer1": {
                "columns": {
                  "col1": {
                    "label": "Duration",
                    "dataType": "number",
                    "operationType": "range",
                    "sourceField": "duration",
                    "isBucketed": true,
                    "scale": "interval",
                    "params": {
                      "type": "histogram",
                      "ranges": [
                        {"from": 0, "to": 1000, "label": "0-1ms"},
                        {"from": 1000, "to": 5000, "label": "1-5ms"},
                        {"from": 5000, "to": 10000, "label": "5-10ms"},
                        {"from": 10000, "to": 50000, "label": "10-50ms"},
                        {"from": 50000, "to": 100000, "label": "50-100ms"},
                        {"from": 100000, "label": ">100ms"}
                      ],
                      "maxBars": 50
                    }
                  },
                  "col2": {
                    "label": "Count",
                    "dataType": "number",
                    "operationType": "count",
                    "isBucketed": false,
                    "scale": "ratio",
                    "sourceField": "___records___"
                  }
                },
                "columnOrder": ["col1", "col2"],
                "incompleteColumns": {}
              }
            }
          }
        },
        "visualization": {
          "legend": {
            "isVisible": true,
            "position": "right"
          },
          "valueLabels": "hide",
          "fittingFunction": "None",
          "preferredSeriesType": "bar_stacked",
          "layers": [
            {
              "layerId": "layer1",
              "accessors": ["col2"],
              "position": "top",
              "seriesType": "bar_stacked",
              "showGridlines": false,
              "layerType": "data",
              "xAccessor": "col1"
            }
          ]
        },
        "query": {
          "query": "",
          "language": "kuery"
        },
        "filters": []
      },
      "references": [
        {
          "type": "index-pattern",
          "id": "'"$JAEGER_SPAN_DATA_VIEW_ID"'",
          "name": "indexpattern-datasource-layer-layer1"
        }
      ]
    }
  }')

VIZ_2_ID=$(echo $VIZ_2_RESPONSE | jq -r '.id')
echo "✅ 建立成功 (ID: $VIZ_2_ID)"

echo ""
echo "步驟 3/5: 建立「依服務的請求數」（Lens）..."
VIZ_3_RESPONSE=$(curl -X POST "$KIBANA_URL/api/saved_objects/lens" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Traces - 依服務的請求數",
      "description": "顯示各服務的請求數量",
      "visualizationType": "lnsPie",
      "state": {
        "datasourceStates": {
          "formBased": {
            "layers": {
              "layer1": {
                "columns": {
                  "col1": {
                    "label": "Service",
                    "dataType": "string",
                    "operationType": "terms",
                    "sourceField": "process.serviceName.keyword",
                    "isBucketed": true,
                    "scale": "ordinal",
                    "params": {
                      "size": 10,
                      "orderBy": {
                        "type": "column",
                        "columnId": "col2"
                      },
                      "orderDirection": "desc"
                    }
                  },
                  "col2": {
                    "label": "Count",
                    "dataType": "number",
                    "operationType": "count",
                    "isBucketed": false,
                    "scale": "ratio",
                    "sourceField": "___records___"
                  }
                },
                "columnOrder": ["col1", "col2"],
                "incompleteColumns": {}
              }
            }
          }
        },
        "visualization": {
          "shape": "donut",
          "layers": [
            {
              "layerId": "layer1",
              "primaryGroups": ["col1"],
              "metrics": ["col2"],
              "numberDisplay": "percent",
              "categoryDisplay": "default",
              "legendDisplay": "default",
              "nestedLegend": false
            }
          ]
        },
        "query": {
          "query": "",
          "language": "kuery"
        },
        "filters": []
      },
      "references": [
        {
          "type": "index-pattern",
          "id": "'"$JAEGER_SPAN_DATA_VIEW_ID"'",
          "name": "indexpattern-datasource-layer-layer1"
        }
      ]
    }
  }')

VIZ_3_ID=$(echo $VIZ_3_RESPONSE | jq -r '.id')
echo "✅ 建立成功 (ID: $VIZ_3_ID)"

echo ""
echo "步驟 4/5: 建立「日誌等級分佈」（Lens）..."
VIZ_4_RESPONSE=$(curl -X POST "$KIBANA_URL/api/saved_objects/lens" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Logs - 日誌等級分佈",
      "description": "顯示日誌等級的分佈",
      "visualizationType": "lnsPie",
      "state": {
        "datasourceStates": {
          "formBased": {
            "layers": {
              "layer1": {
                "columns": {
                  "col1": {
                    "label": "Log Level",
                    "dataType": "string",
                    "operationType": "terms",
                    "sourceField": "log.level.keyword",
                    "isBucketed": true,
                    "scale": "ordinal",
                    "params": {
                      "size": 10,
                      "orderBy": {
                        "type": "column",
                        "columnId": "col2"
                      },
                      "orderDirection": "desc"
                    }
                  },
                  "col2": {
                    "label": "Count",
                    "dataType": "number",
                    "operationType": "count",
                    "isBucketed": false,
                    "scale": "ratio",
                    "sourceField": "___records___"
                  }
                },
                "columnOrder": ["col1", "col2"],
                "incompleteColumns": {}
              }
            }
          }
        },
        "visualization": {
          "shape": "pie",
          "layers": [
            {
              "layerId": "layer1",
              "primaryGroups": ["col1"],
              "metrics": ["col2"],
              "numberDisplay": "percent",
              "categoryDisplay": "default",
              "legendDisplay": "default",
              "nestedLegend": false
            }
          ]
        },
        "query": {
          "query": "",
          "language": "kuery"
        },
        "filters": []
      },
      "references": [
        {
          "type": "index-pattern",
          "id": "'"$OTEL_LOGS_DATA_VIEW_ID"'",
          "name": "indexpattern-datasource-layer-layer1"
        }
      ]
    }
  }')

VIZ_4_ID=$(echo $VIZ_4_RESPONSE | jq -r '.id')
echo "✅ 建立成功 (ID: $VIZ_4_ID)"

echo ""
echo "步驟 5/5: 建立「依服務分組的日誌量」（Lens）..."
VIZ_5_RESPONSE=$(curl -X POST "$KIBANA_URL/api/saved_objects/lens" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "Logs - 依服務分組的日誌量",
      "description": "顯示各服務的日誌數量趨勢",
      "visualizationType": "lnsXY",
      "state": {
        "datasourceStates": {
          "formBased": {
            "layers": {
              "layer1": {
                "columns": {
                  "col1": {
                    "label": "@timestamp",
                    "dataType": "date",
                    "operationType": "date_histogram",
                    "sourceField": "@timestamp",
                    "isBucketed": true,
                    "scale": "interval",
                    "params": {
                      "interval": "auto"
                    }
                  },
                  "col2": {
                    "label": "Count",
                    "dataType": "number",
                    "operationType": "count",
                    "isBucketed": false,
                    "scale": "ratio",
                    "sourceField": "___records___"
                  },
                  "col3": {
                    "label": "Service",
                    "dataType": "string",
                    "operationType": "terms",
                    "sourceField": "Application.keyword",
                    "isBucketed": true,
                    "scale": "ordinal",
                    "params": {
                      "size": 5,
                      "orderBy": {
                        "type": "column",
                        "columnId": "col2"
                      },
                      "orderDirection": "desc"
                    }
                  }
                },
                "columnOrder": ["col1", "col3", "col2"],
                "incompleteColumns": {}
              }
            }
          }
        },
        "visualization": {
          "legend": {
            "isVisible": true,
            "position": "right"
          },
          "valueLabels": "hide",
          "fittingFunction": "None",
          "preferredSeriesType": "bar_stacked",
          "layers": [
            {
              "layerId": "layer1",
              "accessors": ["col2"],
              "position": "top",
              "seriesType": "bar_stacked",
              "showGridlines": false,
              "layerType": "data",
              "xAccessor": "col1",
              "splitAccessor": "col3"
            }
          ]
        },
        "query": {
          "query": "",
          "language": "kuery"
        },
        "filters": []
      },
      "references": [
        {
          "type": "index-pattern",
          "id": "'"$OTEL_LOGS_DATA_VIEW_ID"'",
          "name": "indexpattern-datasource-layer-layer1"
        }
      ]
    }
  }')

VIZ_5_ID=$(echo $VIZ_5_RESPONSE | jq -r '.id')
echo "✅ 建立成功 (ID: $VIZ_5_ID)"

echo ""
echo "步驟 6/6: 建立 Dashboard..."
DASHBOARD_RESPONSE=$(curl -X POST "$KIBANA_URL/api/saved_objects/dashboard" \
  -H "$HEADERS" -H "$XSRF_HEADER" \
  -d '{
    "attributes": {
      "title": "OpenTelemetry 可觀測性總覽 v2",
      "description": "整合 Traces 和 Logs 的可觀測性儀表板（使用 Lens）",
      "panelsJSON": "[{\"version\":\"8.17.1\",\"type\":\"lens\",\"gridData\":{\"x\":0,\"y\":0,\"w\":24,\"h\":15,\"i\":\"1\"},\"panelIndex\":\"1\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_1\"},{\"version\":\"8.17.1\",\"type\":\"lens\",\"gridData\":{\"x\":24,\"y\":0,\"w\":24,\"h\":15,\"i\":\"2\"},\"panelIndex\":\"2\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_2\"},{\"version\":\"8.17.1\",\"type\":\"lens\",\"gridData\":{\"x\":0,\"y\":15,\"w\":16,\"h\":15,\"i\":\"3\"},\"panelIndex\":\"3\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_3\"},{\"version\":\"8.17.1\",\"type\":\"lens\",\"gridData\":{\"x\":16,\"y\":15,\"w\":16,\"h\":15,\"i\":\"4\"},\"panelIndex\":\"4\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_4\"},{\"version\":\"8.17.1\",\"type\":\"lens\",\"gridData\":{\"x\":32,\"y\":15,\"w\":16,\"h\":15,\"i\":\"5\"},\"panelIndex\":\"5\",\"embeddableConfig\":{\"enhancements\":{}},\"panelRefName\":\"panel_5\"}]",
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
        "type": "lens",
        "id": "'"$VIZ_1_ID"'"
      },
      {
        "name": "panel_2",
        "type": "lens",
        "id": "'"$VIZ_2_ID"'"
      },
      {
        "name": "panel_3",
        "type": "lens",
        "id": "'"$VIZ_3_ID"'"
      },
      {
        "name": "panel_4",
        "type": "lens",
        "id": "'"$VIZ_4_ID"'"
      },
      {
        "name": "panel_5",
        "type": "lens",
        "id": "'"$VIZ_5_ID"'"
      }
    ]
  }')

DASHBOARD_ID=$(echo $DASHBOARD_RESPONSE | jq -r '.id')
echo "✅ Dashboard 建立成功 (ID: $DASHBOARD_ID)"

echo ""
echo "=========================================="
echo "✅ 完成！"
echo "=========================================="
echo ""
echo "Dashboard URL:"
echo "http://localhost:5601/app/dashboards#/view/$DASHBOARD_ID"
echo ""
echo "包含的視覺化（使用 Lens）："
echo "  📈 Traces - 請求數量趨勢"
echo "  📊 Traces - 服務延遲分佈"
echo "  🥧 Traces - 依服務的請求數"
echo "  🥧 Logs - 日誌等級分佈"
echo "  📈 Logs - 依服務分組的日誌量"
echo ""
echo "註：Lens 是 Kibana 的現代化視覺化引擎，更穩定且易用"
echo ""
