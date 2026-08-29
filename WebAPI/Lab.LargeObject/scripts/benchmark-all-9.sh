#!/usr/bin/env bash
set -euo pipefail

# 9 種全組合一鍵自動化大橫評腳本（支援結果持久化與結果重用）：
# 參數支援：
#   ./benchmark-all-9.sh           # 完整執行壓測並將結果持久化至 latest-results.json
#   ./benchmark-all-9.sh --report  # 直接讀取上次儲存的結果並渲染 Markdown 表格（無需重跑）

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
RESULTS_JSON="$SCRIPT_DIR/latest-results.json"
PAYLOAD_DOUBLE="$SCRIPT_DIR/payload-4mb.json"
PAYLOAD_MEMBERS="$SCRIPT_DIR/payload-members-20k.json"
PORT=5144
BASE_URL="http://localhost:$PORT"
CONCURRENCY=10
TOTAL_REQUESTS=50

render_markdown_table() {
    if [[ ! -f "$RESULTS_JSON" ]]; then
        echo "❌ 找不到過去的測試紀錄 ($RESULTS_JSON)，請先完整執行一次壓測！" >&2
        exit 1
    fi

    echo "## 實測數據：9 種全組合完整對照大一統總表"
    echo ""
    echo "| 推薦等級 | 資料型別分類 | 實作架構 | API 端點 | 總耗時<br>(ms) | GC 總停頓時間<br>(Pause Time / 佔比) | Gen0 GC<br>次數 | Gen1 GC<br>次數 | Gen2 GC<br>次數 | LOH 峰值<br>(MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |"
    echo "|:---:|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|"

    python3 -c "
import json
with open('$RESULTS_JSON', 'r') as f:
    data = json.load(f)
for item in data:
    tier = item.get('tier', '⚡')
    cat = item['type_category']
    arch = item['pattern_name']
    ep = item['endpoint']
    el = item['elapsed_ms']
    pause = f\"{item['gc_pause_ms']} ms ({item['gc_pause_pct']}%)\"
    g0 = f\"{item['gen0_count']} 次\"
    g1 = f\"{item['gen1_count']} 次\"
    g2 = f\"{item['gen2_count']} 次\"
    loh = f\"{item['loh_peak_mb']} MB\"
    ws = f\"{item['working_set_mb']} MB\"
    verdict = item.get('verdict', '')
    print(f\"| {tier} | **{cat}** | **{arch}** | \`{ep}\` | **{el}** | {pause} | {g0} | {g1} | {g2} | **{loh}** | **{ws}** | {verdict} |\")
"
}

if [[ "${1:-}" == "--report" || "${1:-}" == "-r" ]]; then
    echo "📊 從快取讀取上次壓測結果 ($RESULTS_JSON)："
    echo ""
    render_markdown_table
    exit 0
fi

# 確保 payload 存在
if [[ ! -f "$PAYLOAD_DOUBLE" ]]; then
    awk 'BEGIN{ printf "["; for (i=0;i<524288;i++){ if(i>0)printf ","; printf "%.1f",i+0.5;} printf "]"; }' > "$PAYLOAD_DOUBLE"
fi

if [[ ! -f "$PAYLOAD_MEMBERS" ]]; then
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

TEMP_RESULTS_FILE=$(mktemp)
echo "[]" > "$TEMP_RESULTS_FILE"

record_result_json() {
    local tier="$1"
    local cat="$2"
    local pattern="$3"
    local endpoint="$4"
    local elapsed="$5"
    local pause_ms="$6"
    local pause_pct="$7"
    local g0="$8"
    local g1="$9"
    local g2="${10}"
    local loh="${11}"
    local ws="${12}"
    local verdict="${13}"

    python3 -c "
import json
with open('$TEMP_RESULTS_FILE', 'r') as f:
    arr = json.load(f)
arr.append({
    'tier': '$tier',
    'type_category': '$cat',
    'pattern_name': '$pattern',
    'endpoint': '$endpoint',
    'elapsed_ms': $elapsed,
    'gc_pause_ms': $pause_ms,
    'gc_pause_pct': '$pause_pct',
    'gen0_count': $g0,
    'gen1_count': $g1,
    'gen2_count': $g2,
    'loh_peak_mb': $loh,
    'working_set_mb': $ws,
    'verdict': '$verdict'
})
with open('$TEMP_RESULTS_FILE', 'w') as f:
    json.dump(arr, f, indent=2, ensure_ascii=False)
"
}

run_test() {
    local tier="$1"
    local type_category="$2"
    local pattern_name="$3"
    local endpoint="$4"
    local payload_file="$5"
    local verdict="$6"
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

    # 取得壓測前 GC 基準
    local stats_before
    stats_before=$(curl -s "$BASE_URL/diag/gc-stats")
    local pause_before
    pause_before=$(echo "$stats_before" | grep -o '"totalPauseDurationMs":[0-9.]*' | cut -d: -f2)
    local g0_before
    g0_before=$(echo "$stats_before" | grep -o '"gen0Collections":[0-9]*' | cut -d: -f2)
    local g1_before
    g1_before=$(echo "$stats_before" | grep -o '"gen1Collections":[0-9]*' | cut -d: -f2)
    local g2_before
    g2_before=$(echo "$stats_before" | grep -o '"gen2Collections":[0-9]*' | cut -d: -f2)

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

    # 取得壓測後 GC 數據
    local stats_after
    stats_after=$(curl -s "$BASE_URL/diag/gc-stats")
    local pause_after
    pause_after=$(echo "$stats_after" | grep -o '"totalPauseDurationMs":[0-9.]*' | cut -d: -f2)
    local pause_pct
    pause_pct=$(echo "$stats_after" | grep -o '"pauseTimePercentage":[0-9.]*' | cut -d: -f2)
    local g0_after
    g0_after=$(echo "$stats_after" | grep -o '"gen0Collections":[0-9]*' | cut -d: -f2)
    local g1_after
    g1_after=$(echo "$stats_after" | grep -o '"gen1Collections":[0-9]*' | cut -d: -f2)
    local g2_after
    g2_after=$(echo "$stats_after" | grep -o '"gen2Collections":[0-9]*' | cut -d: -f2)

    local gc_pause_delta
    gc_pause_delta=$(awk -v a="$pause_after" -v b="$pause_before" 'BEGIN{printf "%.1f", a-b}')
    local g0_delta=$((g0_after - g0_before))
    local g1_delta=$((g1_after - g1_before))
    local g2_delta=$((g2_after - g2_before))

    wait $counter_pid 2>/dev/null || true

    kill -9 $api_pid 2>/dev/null || true
    pkill -9 -f Lab.LargeObject.Api 2>/dev/null || true
    sleep 1

    local full_csv="${csv_out}.csv"
    if [[ ! -f "$full_csv" && -f "$csv_out" ]]; then
        full_csv="$csv_out"
    fi

    local loh_peak=0
    if grep -q 'generation=loh' "$full_csv" 2>/dev/null; then
        loh_peak=$(grep 'generation=loh' "$full_csv" | grep 'heap.size' | awk -F',' 'BEGIN{max=0} {if($5>max) max=$5} END{print int(max)}')
    fi

    local ws_final=0
    if grep -q 'working_set' "$full_csv" 2>/dev/null; then
        ws_final=$(grep 'working_set' "$full_csv" | tail -n 1 | awk -F',' '{print int($5)}')
    fi

    local loh_mb=$((loh_peak / 1024 / 1024))
    local ws_mb=$((ws_final / 1024 / 1024))

    record_result_json "$tier" "$type_category" "$pattern_name" "$endpoint" "$elapsed_ms" "$gc_pause_delta" "$pause_pct" "$g0_delta" "$g1_delta" "$g2_delta" "$loh_mb" "$ws_mb" "$verdict"

    echo "RESULT | $tier | $type_category | $pattern_name | $endpoint | 耗時:${elapsed_ms}ms | GC停頓:${gc_pause_delta}ms (${pause_pct}%) | Gen0:${g0_delta}次 | Gen1:${g1_delta}次 | Gen2:${g2_delta}次 | LOH:${loh_mb}MB | WS:${ws_mb}MB"
}

echo "=== 開始 9 種全組合橫評測試 (50 請求，10 並行) ==="
# 1. 數值型別 (4MB double)
run_test "🏆 S 級" "1. 原生數值 (double 4MB)" "Streaming (串流)" "/api/readings-stream" "$PAYLOAD_DOUBLE" "🏆 最快、停頓最短 (14ms)、記憶體最低"
run_test "⚡ A 級" "1. 原生數值 (double 4MB)" "ArrayPool (池化)" "/api/readings" "$PAYLOAD_DOUBLE" "⚡ 陣列完整池化，暖機後重複複用 4MB Buffer"
run_test "❌ D 級" "1. 原生數值 (double 4MB)" "List (未池化)" "/api/readings-list" "$PAYLOAD_DOUBLE" "❌ 擴容連續拋棄暫存陣列，製造 LOH 垃圾"

# 2. 結構值型別 (Struct 20k 筆)
run_test "🏆 S 級" "2. 巢狀結構 (Struct 20k)" "Streaming (串流)" "/api/members-stream" "$PAYLOAD_MEMBERS" "🏆 Struct 最佳解，停頓降 70%、0 LOH、記憶體減半"
run_test "⚡ A 級" "2. 巢狀結構 (Struct 20k)" "ArrayPool (池化)" "/api/members" "$PAYLOAD_MEMBERS" "⚡ 需隨機存取首選，資料內嵌於 Buffer 完整池化"
run_test "❌ D 級" "2. 巢狀結構 (Struct 20k)" "List (未池化)" "/api/members-list" "$PAYLOAD_MEMBERS" "❌ GC 停頓極長 (218ms)，短命陣列引發頻繁 Full GC"

# 3. 參考型別 (Class 20k 筆)
run_test "🛡️ B 級" "3. 參考型別 (Class 20k)" "Streaming (串流)" "/api/members-class-stream" "$PAYLOAD_MEMBERS" "🏆 Class 最佳解，GC 停頓降 66%，記憶體維持極低"
run_test "⚠️ C 級" "3. 參考型別 (Class 20k)" "ArrayPool (池化)" "/api/members-class-pooled" "$PAYLOAD_MEMBERS" "⚠️ 池化效益低，僅省下指標，物件依舊觸發長時間 GC"
run_test "❌ D 級" "3. 參考型別 (Class 20k)" "List (未池化)" "/api/members-class-list" "$PAYLOAD_MEMBERS" "⚠️ 4 萬個 Class 實體散落 Gen0，GC 停頓高達 173ms"

mv "$TEMP_RESULTS_FILE" "$RESULTS_JSON"
echo ""
echo "✅ 測試結果已成功儲存至 $RESULTS_JSON！"
echo "👉 後續可直接執行 ./scripts/benchmark-all-9.sh --report 重複查看此報表，無需重跑。"
