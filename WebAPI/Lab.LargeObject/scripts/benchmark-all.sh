#!/usr/bin/env bash
set -euo pipefail

# 三種寫法完整對照實驗腳本：
# 1. List<MemberAccount> (未池化)
# 2. PooledArray<MemberAccount> (ArrayPool)
# 3. IAsyncEnumerable<MemberAccount> (串流解析)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PAYLOAD="$SCRIPT_DIR/payload-members-20k.json"
PORT=5139
BASE_URL="http://localhost:$PORT"
CONCURRENCY=10
TOTAL_REQUESTS=50

# 確保 payload 存在
if [[ ! -f "$PAYLOAD" ]]; then
    echo "產生 20,000 筆 MemberAccount 測試 Payload..."
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
    }' > "$PAYLOAD"
fi

DOTNET_COUNTERS="dotnet-counters"
if ! command -v dotnet-counters >/dev/null 2>&1; then
    DOTNET_COUNTERS="$HOME/.dotnet/tools/dotnet-counters"
fi

run_single_benchmark() {
    local name="$1"
    local endpoint="$2"
    local csv_out="$SCRIPT_DIR/bench-${name}-$(date +%s).csv"

    echo "=================================================="
    echo "正在測試：$name ($endpoint)"
    echo "=================================================="

    # 1. 啟動全新 API 實體
    ASPNETCORE_URLS="$BASE_URL" dotnet run --project "$ROOT_DIR/src/Lab.LargeObject.Api" --no-launch-profile > /dev/null 2>&1 &
    local api_pid=$!

    # 等待 API 就緒
    while ! curl -s "$BASE_URL/" >/dev/null 2>&1; do
        sleep 0.5
    done

    # 找到真實 process
    local target_pid
    target_pid=$(pgrep -P $api_pid -f Lab.LargeObject.Api || echo "$api_pid")

    # 2. 啟動計數器記錄 (15 秒)
    "$DOTNET_COUNTERS" collect -p "$target_pid" \
        --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.gc.collections,dotnet.gc.heap.total_allocated,dotnet.process.memory.working_set]" \
        --format csv --output "$csv_out" --duration "00:00:00:15" >/dev/null 2>&1 &
    local counter_pid=$!

    sleep 2 # 暖身計數器

    # 3. 發送壓測請求
    local start_time
    start_time=$(date +%s%N)

    seq 1 "$TOTAL_REQUESTS" | xargs -P "$CONCURRENCY" -I{} \
        curl -s -o /dev/null -w "%{http_code}\n" \
            -X POST "$BASE_URL$endpoint" \
            -H "Content-Type: application/json" \
            --data-binary "@$PAYLOAD" \
        | sort | uniq -c

    local end_time
    end_time=$(date +%s%N)
    local elapsed_ms=$(( (end_time - start_time) / 1000000 ))

    # 等待計數器結束
    wait $counter_pid 2>/dev/null || true

    # 4. 關閉 API
    kill -9 $api_pid 2>/dev/null || true
    pkill -9 -f Lab.LargeObject.Api 2>/dev/null || true
    sleep 1

    # 5. 分析 CSV
    local full_csv="${csv_out}.csv"
    if [[ ! -f "$full_csv" && -f "$csv_out" ]]; then
        full_csv="$csv_out"
    fi

    local gen2_sum=0
    if grep -q 'generation=gen2' "$full_csv" 2>/dev/null; then
        gen2_sum=$(grep 'generation=gen2' "$full_csv" | awk -F',' '{sum+=$5} END{print int(sum)}')
    fi

    local loh_peak=0
    if grep -q 'generation=loh' "$full_csv" 2>/dev/null; then
        loh_peak=$(grep 'generation=loh' "$full_csv" | grep 'heap.size' | awk -F',' 'BEGIN{max=0} {if($5>max) max=$5} END{print int(max)}')
    fi

    local loh_final=0
    if grep -q 'generation=loh' "$full_csv" 2>/dev/null; then
        loh_final=$(grep 'generation=loh' "$full_csv" | grep 'heap.size' | tail -n 1 | awk -F',' '{print int($5)}')
    fi

    local alloc_final=0
    if grep -q 'heap.total_allocated' "$full_csv" 2>/dev/null; then
        alloc_final=$(grep 'heap.total_allocated' "$full_csv" | tail -n 1 | awk -F',' '{print int($5)}')
    fi

    local ws_final=0
    if grep -q 'working_set' "$full_csv" 2>/dev/null; then
        ws_final=$(grep 'working_set' "$full_csv" | tail -n 1 | awk -F',' '{print int($5)}')
    fi

    echo "--- 測試結果 ---"
    echo "耗時: ${elapsed_ms} ms"
    echo "Gen2 GC 總次數: ${gen2_sum}"
    echo "LOH 峰值: $((loh_peak / 1024 / 1024)) MB (${loh_peak} bytes)"
    echo "LOH 最終值: $((loh_final / 1024 / 1024)) MB (${loh_final} bytes)"
    echo "累積總配置量: $((alloc_final / 1024 / 1024)) MB (${alloc_final} bytes)"
    echo "Working Set (RSS): $((ws_final / 1024 / 1024)) MB (${ws_final} bytes)"
    echo ""
}

echo "開始全架構對照實驗..."
run_single_benchmark "list" "/api/members-list"
run_single_benchmark "pooled" "/api/members"
run_single_benchmark "stream" "/api/members-stream"

