#!/usr/bin/env bash
set -euo pipefail

# Response 大型回傳資料 4 種架構大橫評腳本（支援結果持久化與結果重用）：
# 1. /api/export-list   (List 未池化)
# 2. /api/export-bytes  (SerializeToUtf8Bytes / byte[])
# 3. /api/export-pooled (ArrayPool 池化)
# 4. /api/export-stream (IAsyncEnumerable 串流回傳)
#
# 參數支援：
#   ./benchmark-response.sh           # 完整執行 4 組 Response 壓測並持久化至 latest-response-results.json
#   ./benchmark-response.sh --report  # 直接讀取上次儲存的結果並渲染 Markdown 表格（無需重跑）

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
RESULTS_JSON="$SCRIPT_DIR/latest-response-results.json"
PORT=5146
BASE_URL="http://localhost:$PORT"
CONCURRENCY=10
TOTAL_REQUESTS=50

render_markdown_table() {
    if [[ ! -f "$RESULTS_JSON" ]]; then
        echo "❌ 找不到過去的 Response 測試紀錄 ($RESULTS_JSON)，請先完整執行一次壓測！" >&2
        exit 1
    fi

    echo "## 實測數據：Response（回傳大型資料）4 種架構完整對照大一統總表"
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
    echo "📊 從快取讀取上次 Response 壓測結果 ($RESULTS_JSON)："
    echo ""
    render_markdown_table
    exit 0
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
    local verdict="$5"
    local csv_out="$SCRIPT_DIR/bench-resp-${pattern_name}-$(date +%s).csv"

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
            -X GET "$BASE_URL$endpoint" \
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

echo "=== 開始 Response 大型回傳資料 4 種架構橫評測試 (50 請求，10 並行) ==="

# 1. 串流回傳
run_test "🏆 S 級" "Response 回傳 (20k 筆)" "Streaming (串流回傳)" "/api/export-stream" "🏆 0 LOH、GC 停頓極短、邊產邊傳記憶體極低"

# 2. ArrayPool 池化
run_test "⚡ A 級" "Response 回傳 (20k 筆)" "ArrayPool (池化回傳)" "/api/export-pooled" "⚡ 租用 Buffer 序列化後歸還，避免多次分配"

# 3. List 未池化
run_test "❌ D 級" "Response 回傳 (20k 筆)" "List (未池化回傳)" "/api/export-list" "❌ 每次請求建立大 List 佔據 LOH，引發 GC 停頓"

# 4. SerializeToUtf8Bytes (byte[])
run_test "💥 D 級" "Response 回傳 (20k 筆)" "SerializeToUtf8Bytes (byte[])" "/api/export-bytes" "💥 直接產出 3MB byte[] 丟進 LOH，效能最差"

mv "$TEMP_RESULTS_FILE" "$RESULTS_JSON"
echo ""
echo "✅ 測試結果已成功儲存至 $RESULTS_JSON！"
echo "👉 後續可直接執行 ./scripts/benchmark-response.sh --report 重複查看此報表，無需重跑。"
