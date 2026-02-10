#!/bin/bash

# 清空 Elasticsearch 資料腳本
# 用途: 停止 Elasticsearch 容器、清空資料目錄、重新啟動

set -e  # 遇到錯誤立即退出

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}清空 Elasticsearch 資料${NC}"
echo -e "${YELLOW}========================================${NC}"

# 檢查是否在專案根目錄
if [ ! -f "docker-compose.yml" ]; then
    echo -e "${RED}錯誤: 請在專案根目錄執行此腳本${NC}"
    exit 1
fi

# 確認操作
echo -e "${RED}警告: 此操作將刪除所有 Elasticsearch 資料！${NC}"
echo -e "${YELLOW}包含以下索引資料:${NC}"
echo "  - otel-traces (OpenTelemetry Traces)"
echo "  - otel-logs (OpenTelemetry Logs)"
echo "  - otel-metrics (OpenTelemetry Metrics)"
echo "  - jaeger-* (Jaeger Spans & Services)"
echo "  - Kibana 內部索引"
echo ""
read -p "確定要繼續嗎? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo -e "${YELLOW}操作已取消${NC}"
    exit 0
fi

echo ""
echo -e "${YELLOW}[1/4] 停止 Elasticsearch 容器...${NC}"
docker compose stop elasticsearch
echo -e "${GREEN}✓ Elasticsearch 已停止${NC}"

echo ""
echo -e "${YELLOW}[2/4] 清空資料目錄...${NC}"
if [ -d "./data/elasticsearch" ]; then
    # 檢查目錄權限，可能需要 sudo
    if [ -w "./data/elasticsearch" ]; then
        rm -rf ./data/elasticsearch/*
        echo -e "${GREEN}✓ 資料目錄已清空${NC}"
    else
        echo -e "${YELLOW}需要 sudo 權限來刪除資料...${NC}"
        sudo rm -rf ./data/elasticsearch/*
        echo -e "${GREEN}✓ 資料目錄已清空 (使用 sudo)${NC}"
    fi
else
    echo -e "${YELLOW}資料目錄不存在，跳過清空步驟${NC}"
fi

echo ""
echo -e "${YELLOW}[3/4] 重新啟動 Elasticsearch...${NC}"
docker compose start elasticsearch
echo -e "${GREEN}✓ Elasticsearch 已啟動${NC}"

echo ""
echo -e "${YELLOW}[4/4] 等待 Elasticsearch 健康檢查通過...${NC}"
echo -e "${YELLOW}(最多等待 120 秒)${NC}"

# 等待健康檢查
MAX_RETRIES=24  # 24 * 5 秒 = 120 秒
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if curl -s http://localhost:9200/_cluster/health | grep -q '"status":"green"\|"status":"yellow"'; then
        echo -e "${GREEN}✓ Elasticsearch 健康檢查通過${NC}"
        break
    fi

    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo -n "."
    sleep 5
done

echo ""

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo -e "${RED}✗ Elasticsearch 健康檢查超時${NC}"
    echo -e "${YELLOW}請執行以下命令檢查狀態:${NC}"
    echo "  docker logs elasticsearch"
    exit 1
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Elasticsearch 資料清空完成！${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}後續步驟:${NC}"
echo "  1. 執行 ./scripts/setup-elasticsearch-ilm.sh (設定 ILM 策略)"
echo "  2. 執行 ./scripts/setup-kibana-index-patterns.sh (建立 Kibana Data Views)"
echo "  3. 重新啟動其他服務: docker compose restart"
echo ""
