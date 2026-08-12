#!/usr/bin/env bash
set -euo pipefail

# 對指定端點發送大量並行的大陣列 request，製造 LOH 配置壓力。
# 用法：./load-test.sh <base-url> <endpoint-path> [concurrency] [total-requests]
# 範例：
#   ./load-test.sh http://localhost:5080 /api/readings 20 500

BASE_URL="${1:?請提供 base URL，例如 http://localhost:5080}"
ENDPOINT="${2:?請提供端點路徑，例如 /api/readings}"
CONCURRENCY="${3:-10}"
TOTAL_REQUESTS="${4:-200}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PAYLOAD_FILE="$SCRIPT_DIR/payload-1mb.json"

if [[ ! -f "$PAYLOAD_FILE" ]]; then
    echo "產生 1MB 測試 payload（131072 個 double）：$PAYLOAD_FILE"
    awk 'BEGIN{
        printf "[";
        for (i = 0; i < 131072; i++) {
            if (i > 0) printf ",";
            printf "%.1f", i + 0.5;
        }
        printf "]";
    }' > "$PAYLOAD_FILE"
fi

echo "開始壓測：$BASE_URL$ENDPOINT"
echo "並行數：$CONCURRENCY，總請求數：$TOTAL_REQUESTS"
echo "----"

seq 1 "$TOTAL_REQUESTS" | xargs -P "$CONCURRENCY" -I{} \
    curl -s -o /dev/null -w "%{http_code}\n" \
        -X POST "$BASE_URL$ENDPOINT" \
        -H "Content-Type: application/json" \
        --data-binary "@$PAYLOAD_FILE" \
    | sort | uniq -c

echo "----"
echo "壓測結束"
