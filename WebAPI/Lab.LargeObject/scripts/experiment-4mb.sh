#!/usr/bin/env bash
set -euo pipefail

# 4MB LOH 對照實驗腳本
# 產生 524,288 個 double (約 4.19MB 記憶體，JSON body 約 4.3MB)
# 分別對 /api/readings-list 與 /api/readings 施壓並觀察 GC 行為。

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PAYLOAD_4MB="$SCRIPT_DIR/payload-4mb.json"
BASE_URL="${1:-http://localhost:5138}"
CONCURRENCY="${2:-10}"
TOTAL_REQUESTS="${3:-100}"

# 1. 產生 4MB payload
if [[ ! -f "$PAYLOAD_4MB" ]]; then
    echo "正在產生 4MB 測試 Payload（524,288 個 double）..."
    awk 'BEGIN{
        printf "[";
        for (i = 0; i < 524288; i++) {
            if (i > 0) printf ",";
            printf "%.1f", i + 0.5;
        }
        printf "]";
    }' > "$PAYLOAD_4MB"
    echo "Payload 已產生：$PAYLOAD_4MB ($(du -h "$PAYLOAD_4MB" | cut -f1))"
else
    echo "使用現有 4MB Payload：$PAYLOAD_4MB ($(du -h "$PAYLOAD_4MB" | cut -f1))"
fi

echo "=================================================="
echo "4MB 負載實驗"
echo "目標伺服器：$BASE_URL"
echo "並行數：$CONCURRENCY，總請求數：$TOTAL_REQUESTS"
echo "=================================================="

run_test() {
    local endpoint="$1"
    local name="$2"

    echo ""
    echo ">>> 開始測試：$name ($endpoint)"
    echo "--- 發送請求中 ---"
    
    local start_time
    start_time=$(date +%s%N)

    seq 1 "$TOTAL_REQUESTS" | xargs -P "$CONCURRENCY" -I{} \
        curl -s -o /dev/null -w "%{http_code}\n" \
            -X POST "$BASE_URL$endpoint" \
            -H "Content-Type: application/json" \
            --data-binary "@$PAYLOAD_4MB" \
        | sort | uniq -c

    local end_time
    end_time=$(date +%s%N)
    local elapsed_ms=$(( (end_time - start_time) / 1000000 ))
    echo "--- 測試完成，耗時：${elapsed_ms} ms ---"
}

# 執行對照測試
if [[ "${4:-all}" == "list" ]]; then
    run_test "/api/readings-list" "List<double> (未池化 4MB)"
elif [[ "${4:-all}" == "pooled" ]]; then
    run_test "/api/readings" "PooledArray<double> (ArrayPool 4MB)"
else
    echo "提示：可先在另一個終端機執行 ./scripts/observe-counters.sh Lab.LargeObject.Api 60 觀察指標"
    run_test "/api/readings-list" "List<double> (未池化 4MB)"
    echo ""
    echo "休息 3 秒..."
    sleep 3
    run_test "/api/readings" "PooledArray<double> (ArrayPool 4MB)"
fi

echo ""
echo "=================================================="
echo "實驗結束"
echo "=================================================="
