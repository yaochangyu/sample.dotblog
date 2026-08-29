#!/usr/bin/env bash
set -euo pipefail

# 9 種全組合一鍵自動化大橫評腳本：
# 3 種資料型別 (double, Struct, Class) × 3 種架構 (List, ArrayPool, Streaming)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PAYLOAD_DOUBLE="$SCRIPT_DIR/payload-4mb.json"
PAYLOAD_MEMBERS="$SCRIPT_DIR/payload-members-20k.json"
PORT=5141
BASE_URL="http://localhost:$PORT"
CONCURRENCY=10
TOTAL_REQUESTS=50

# 確保 payload 存在
if [[ ! -f "$PAYLOAD_DOUBLE" ]]; then
    echo "產生 4MB double 測試 Payload..."
    awk 'BEGIN{ printf "["; for (i=0;i<524288;i++){ if(i>0)printf ","; printf "%.1f",i+0.5;} printf "]"; }' > "$PAYLOAD_DOUBLE"
fi

if [[ ! -f "$PAYLOAD_MEMBERS" ]]; then
    echo "產生 20,000 筆 MemberAccount 測試 Payload..."
    awk 'BEGIN{
        printf "["; statuses[0]="Active"; statuses[1]="Suspended"; statuses[2]="Deleted";
        for (i=0;i<20000;i++){
            if(i>0)printf ","; st=statuses[i%3]; phone=(i%2==0)?sprintf("\"09%08d\"",i):"null";
            printf "{\"memberId\":%d,\"account\":\"member%06d\",\"displayName\":\"會員 %d\",\"status\":\"%s\",\"contact\":{\"email\":\"member%06d@example.com\",\"phoneNumber\":%s},\"createdAt\":\"2026-08-29T00:00:00Z\"}", i, i, i, st, i, phone;
        }
        printf "]";
    }' > "$PAYLOAD_MEMBERS"
fi

DOTNET_COUNTERS="dotnet-counters"
if ! command -v dotnet-counters >/dev/null 2>&1; then
    DOTNET_COUNTERS="$HOME/.dotnet/tools/dotnet-counters"
fi

run_test() {
    local type_category="$1"
    local pattern_name="$2"
    local endpoint="$3"
    local payload_file="$4"
    local csv_out="$SCRIPT_DIR/bench-9-${type_category}-${pattern_name}-$(date +%s).csv"

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
            --data-binary "@$payload_file" \
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

    local ws_final=0
    if grep -q 'working_set' "$full_csv" 2>/dev/null; then
        ws_final=$(grep 'working_set' "$full_csv" | tail -n 1 | awk -F',' '{print int($5)}')
    fi

    echo "RESULT | $type_category | $pattern_name | $endpoint | ${elapsed_ms}ms | Gen0:${gen0_sum} | Gen2:${gen2_sum} | LOH_Peak:$((loh_peak/1024/1024))MB | WS:$((ws_final/1024/1024))MB"
}

echo "=== 開始 9 種全組合橫評測試 (50 請求，10 並行) ==="
# 1. 數值型別 (4MB double)
run_test "1.原生數值 (double 4MB)" "List (未池化)" "/api/readings-list" "$PAYLOAD_DOUBLE"
run_test "1.原生數值 (double 4MB)" "ArrayPool (池化)" "/api/readings" "$PAYLOAD_DOUBLE"
run_test "1.原生數值 (double 4MB)" "Streaming (串流)" "/api/readings-stream" "$PAYLOAD_DOUBLE"

# 2. 結構值型別 (Struct 20k 筆)
run_test "2.巢狀結構 (Struct 20k)" "List (未池化)" "/api/members-list" "$PAYLOAD_MEMBERS"
run_test "2.巢狀結構 (Struct 20k)" "ArrayPool (池化)" "/api/members" "$PAYLOAD_MEMBERS"
run_test "2.巢狀結構 (Struct 20k)" "Streaming (串流)" "/api/members-stream" "$PAYLOAD_MEMBERS"

# 3. 參考型別 (Class 20k 筆)
run_test "3.參考型別 (Class 20k)" "List (未池化)" "/api/members-class-list" "$PAYLOAD_MEMBERS"
run_test "3.參考型別 (Class 20k)" "ArrayPool (池化)" "/api/members-class-pooled" "$PAYLOAD_MEMBERS"
run_test "3.參考型別 (Class 20k)" "Streaming (串流)" "/api/members-class-stream" "$PAYLOAD_MEMBERS"
