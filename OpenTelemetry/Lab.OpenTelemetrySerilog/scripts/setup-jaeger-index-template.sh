#!/bin/bash

# 設定 Jaeger Elasticsearch 索引模板
# 解決 serviceName fielddata 錯誤

set -e

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}設定 Jaeger Elasticsearch 索引模板${NC}"
echo -e "${YELLOW}========================================${NC}"

ES_URL="http://localhost:9200"

# 檢查 Elasticsearch 是否運行
echo -e "${YELLOW}檢查 Elasticsearch 連線...${NC}"
if ! curl -s "$ES_URL" > /dev/null; then
    echo -e "${RED}✗ 無法連接到 Elasticsearch${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Elasticsearch 連線正常${NC}"

echo ""
echo -e "${YELLOW}[1/3] 建立 Jaeger Span 索引模板...${NC}"

# Jaeger Span 索引模板
curl -X PUT "$ES_URL/_index_template/jaeger-span-template" \
  -H "Content-Type: application/json" \
  -d '{
  "index_patterns": ["jaeger-jaeger-span-*"],
  "priority": 1,
  "template": {
    "settings": {
      "number_of_shards": 5,
      "number_of_replicas": 1,
      "index.mapping.nested_fields.limit": 50,
      "index.requests.cache.enable": true,
      "index.max_result_window": 10000
    },
    "mappings": {
      "dynamic_templates": [
        {
          "span_tags_map": {
            "path_match": "tag.*",
            "mapping": {
              "type": "keyword",
              "ignore_above": 256
            }
          }
        },
        {
          "process_tags_map": {
            "path_match": "process.tag.*",
            "mapping": {
              "type": "keyword",
              "ignore_above": 256
            }
          }
        }
      ],
      "properties": {
        "traceID": {
          "type": "keyword",
          "ignore_above": 256
        },
        "spanID": {
          "type": "keyword",
          "ignore_above": 256
        },
        "operationName": {
          "type": "keyword",
          "ignore_above": 256
        },
        "startTime": {
          "type": "long"
        },
        "startTimeMillis": {
          "type": "date",
          "format": "epoch_millis"
        },
        "duration": {
          "type": "long"
        },
        "flags": {
          "type": "integer"
        },
        "logs": {
          "type": "nested",
          "dynamic": false
        },
        "process": {
          "properties": {
            "serviceName": {
              "type": "keyword",
              "ignore_above": 256
            },
            "tag": {
              "type": "object"
            },
            "tags": {
              "type": "nested",
              "dynamic": false
            }
          }
        },
        "references": {
          "type": "nested",
          "dynamic": false
        },
        "tags": {
          "type": "nested",
          "dynamic": false
        }
      }
    }
  }
}' && echo -e "${GREEN}✓ Jaeger Span 索引模板建立成功${NC}" || echo -e "${RED}✗ 建立失敗${NC}"

echo ""
echo -e "${YELLOW}[2/3] 建立 Jaeger Service 索引模板...${NC}"

# Jaeger Service 索引模板
curl -X PUT "$ES_URL/_index_template/jaeger-service-template" \
  -H "Content-Type: application/json" \
  -d '{
  "index_patterns": ["jaeger-jaeger-service-*"],
  "priority": 1,
  "template": {
    "settings": {
      "number_of_shards": 5,
      "number_of_replicas": 1,
      "index.requests.cache.enable": true,
      "index.max_result_window": 10000
    },
    "mappings": {
      "properties": {
        "serviceName": {
          "type": "keyword",
          "ignore_above": 256
        },
        "operationName": {
          "type": "keyword",
          "ignore_above": 256
        },
        "spanKind": {
          "type": "keyword",
          "ignore_above": 256
        }
      }
    }
  }
}' && echo -e "${GREEN}✓ Jaeger Service 索引模板建立成功${NC}" || echo -e "${RED}✗ 建立失敗${NC}"

echo ""
echo -e "${YELLOW}[3/3] 刪除現有的錯誤索引並重建...${NC}"

# 刪除現有的 Jaeger 索引（它們使用了錯誤的 mapping）
echo -e "${YELLOW}刪除現有 Jaeger 索引...${NC}"
curl -X DELETE "$ES_URL/jaeger-jaeger-span-*" 2>/dev/null || echo -e "${YELLOW}(沒有 span 索引需要刪除)${NC}"
curl -X DELETE "$ES_URL/jaeger-jaeger-service-*" 2>/dev/null || echo -e "${YELLOW}(沒有 service 索引需要刪除)${NC}"

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}索引模板設定完成！${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}後續步驟:${NC}"
echo "  1. 重啟 Jaeger 容器: docker compose restart jaeger"
echo "  2. 發送新的 trace 資料，索引將自動使用新的模板建立"
echo "  3. 驗證: 訪問 http://localhost:16686 查看 Jaeger UI"
echo ""
echo -e "${YELLOW}驗證索引模板:${NC}"
echo "  curl $ES_URL/_index_template/jaeger-span-template"
echo "  curl $ES_URL/_index_template/jaeger-service-template"
echo ""
