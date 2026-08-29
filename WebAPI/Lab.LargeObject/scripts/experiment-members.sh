#!/usr/bin/env bash
set -euo pipefail

# 複雜型別 (MemberAccount) LOH 對照實驗腳本
# 產生 20,000 筆 MemberAccount (struct 64 bytes * 20000 = 1.28MB 陣列容器，JSON body 約 3.2MB)
# 分別對 /api/members-list 與 /api/members 施壓並觀察 GC 行為。

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PAYLOAD_MEMBERS="$SCRIPT_DIR/payload-members-20k.json"
BASE_URL="${1:-http://localhost:5138}"
CONCURRENCY="${2:-10}"
TOTAL_REQUESTS="${3:-50}"

# 1. 產生 20,000 筆 MemberAccount payload
if [[ ! -f "$PAYLOAD_MEMBERS" ]]; then
    echo "正在產生 20,000 筆 MemberAccount 測試 Payload..."
    awk 'BEGIN{
        printf "[";
        statuses[0]="Active"; statuses[1]="Suspended"; statuses[2]="Deleted";
        for (i = 0; i < 20000; i++) {
            if (i > 0) printf ",";
            st = statuses[i % 3];
            phone = (i % 2 == 0) ? sprintf("\"09%08d\"", i) : "null";
            printf "{\"memberId\":%d,\"account\":\"member%06d\",\"displayName\":\"會員 %d\",\"status\":\"%s\",\"contact\":{\"email\":\"member%06d@example.com\",\"phoneNumber\":%s},\"createdAt\":\"2026-08-29T00:00:00Z\"}", i, i, i, st, i, phone;
        }
        printf "]";
    }' > "$PAYLOAD_MEMBERS"
    echo "Payload 已產生：$PAYLOAD_MEMBERS ($(du -h "$PAYLOAD_MEMBERS" | cut -f1))"
else
    echo "使用現有 Payload：$PAYLOAD_MEMBERS ($(du -h "$PAYLOAD_MEMBERS" | cut -f1))"
fi

echo "=================================================="
echo "複雜型別 (MemberAccount) 負載實驗"
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
            --data-binary "@$PAYLOAD_MEMBERS" \
        | sort | uniq -c

    local end_time
    end_time=$(date +%s%N)
    local elapsed_ms=$(( (end_time - start_time) / 1000000 ))
    echo "--- 測試完成，耗時：${elapsed_ms} ms ---"
}

# 執行對照測試
if [[ "${4:-all}" == "list" ]]; then
    run_test "/api/members-list" "List<MemberAccount> (未池化)"
elif [[ "${4:-all}" == "pooled" ]]; then
    run_test "/api/members" "PooledArray<MemberAccount> (ArrayPool)"
else
    echo "提示：可先在另一個終端機執行 ./scripts/observe-counters.sh Lab.LargeObject.Api 60 觀察指標"
    run_test "/api/members-list" "List<MemberAccount> (未池化)"
    echo ""
    echo "休息 3 秒..."
    sleep 3
    run_test "/api/members" "PooledArray<MemberAccount> (ArrayPool)"
fi

echo ""
echo "=================================================="
echo "實驗結束"
echo "=================================================="
