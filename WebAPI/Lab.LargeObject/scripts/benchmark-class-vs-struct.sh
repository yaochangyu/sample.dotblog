#!/usr/bin/env bash
set -euo pipefail

# Struct vs Class 完整對照實驗腳本
# 測試 6 種組合：
# 1. Struct + List
# 2. Struct + ArrayPool
# 3. Struct + Streaming
# 4. Class + List
# 5. Class + ArrayPool
# 6. Class + Streaming

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PAYLOAD="$SCRIPT_DIR/payload-members-20k.json"
PORT=5140
BASE_URL="http://localhost:$PORT"
CONCURRENCY=10
TOTAL_REQUESTS=50

DOTNET_COUNTERS="dotnet-counters"
if ! command -v dotnet-counters >/dev/null 2>&1; then
    DOTNET_COUNTERS="$HOME/.dotnet/tools/dotnet-counters"
fi

run_test_case() {
    local label="$1"
    local endpoint="$2"
    local csv_out="$SCRIPT_DIR/bench-cvs-${label}-$(date +%s).csv"

    echo "=================================================="
    echo "正在測試：$label ($endpoint)"
    echo "=================================================="

    ASPNETCORE_URLS="$BASE_URL" dotnet run --project "$ROOT_DIR/src/Lab.LargeObject.Api" --no-launch-profile > /dev/null 2>&1 &
    local api_pid=$!

    while ! curl -s "$BASE_URL/" >/dev/null 2>&1; do
        sleep 0.5
    done

    local target_pid
    target_pid=$(pgrep -P $api_pid -f Lab.LargeObject.Api || echo "$api_pid")

    "$DOTNET_COUNTERS" collect -p "$target_pid" \
        --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.gc.collections,dotnet.gc.heap.total_allocated,dotnet.process.memory.working_set]" \
        --format csv --output "$csv_out" --duration "00:00:00:15" >/dev/null 2>&1 &
    local counter_pid=$!

    sleep 2

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

    wait $counter_pid 2>/dev/null || true

    kill -9 $api_pid 2>/dev/null || true
    pkill -9 -f Lab.LargeObject.Api 2>/dev/null || true
    sleep 1

    local full_csv="${csv_out}.csv"
    if [[ ! -f "$full_csv" && -f "$csv_out" ]]; then
        full_csv="$csv_out"
    fi

    local gen2_sum=0
    if grep -q 'generation=gen2' "$full_csv" 2>/dev/null; then
        gen2_sum=$(grep 'generation=gen2' "$full_csv" | awk -F',' '{sum+=$5} END{print int(sum)}')
    fi

    local gen0_sum=0
    if grep -q 'generation=gen0' "$full_csv" 2>/dev/null; then
        gen0_sum=$(grep 'generation=gen0' "$full_csv" | awk -F',' '{sum+=$5} END{print int(sum)}')
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

    echo "--- 測試結果 ($label) ---"
    echo "耗時: ${elapsed_ms} ms"
    echo "Gen0 GC 次數: ${gen0_sum}"
    echo "Gen2 GC 次數: ${gen2_sum}"
    echo "LOH 峰值: $((loh_peak / 1024 / 1024)) MB (${loh_peak} bytes)"
    echo "LOH 最終值: $((loh_final / 1024 / 1024)) MB (${loh_final} bytes)"
    echo "Working Set: $((ws_final / 1024 / 1024)) MB (${ws_final} bytes)"
    echo ""
}

echo "=== 開始 Struct vs Class 完整壓測對比 (20,000 筆，50 請求，10 並行) ==="
run_test_case "Struct_List" "/api/members-list"
run_test_case "Struct_ArrayPool" "/api/members"
run_test_case "Struct_Stream" "/api/members-stream"
run_test_case "Class_List" "/api/members-class-list"
run_test_case "Class_ArrayPool" "/api/members-class-pooled"
run_test_case "Class_Stream" "/api/members-class-stream"
