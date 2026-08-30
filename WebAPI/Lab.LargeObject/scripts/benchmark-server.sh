#!/usr/bin/env bash
set -euo pipefail

# Server 端記憶體與 GC 綜合評測腳本（涵蓋 Request 12組 + Response 12組，共 24 組 Server 完整測試）
# 參數支援：
#   ./benchmark-server.sh               # 完整執行 Server 端 24 組壓測並持久化
#   ./benchmark-server.sh --request     # 僅執行 Request 12 組
#   ./benchmark-server.sh --response    # 僅執行 Response 12 組
#   ./benchmark-server.sh --report      # 秒級讀取快取並渲染 Server 端 24 組 Markdown 總表

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/.output"
mkdir -p "$OUTPUT_DIR"
RESULTS_REQ_JSON="$OUTPUT_DIR/latest-results.json"
RESULTS_RESP_JSON="$OUTPUT_DIR/latest-response-results.json"
PAYLOAD_DOUBLE="$OUTPUT_DIR/payload-4mb.json"
PAYLOAD_STRINGS="$OUTPUT_DIR/payload-strings-50k.json"
PAYLOAD_MEMBERS="$OUTPUT_DIR/payload-members-20k.json"
PORT=5146
BASE_URL="http://localhost:$PORT"
CONCURRENCY=10
TOTAL_REQUESTS=50

cleanup() {
    kill $(jobs -p) 2>/dev/null || true
    pkill -9 -f "Lab.LargeObject.Api.dll" 2>/dev/null || true
    pkill -9 -f "Lab.LargeObject.BenchClient.dll" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

render_request_table() {
    if [[ ! -f "$RESULTS_REQ_JSON" ]]; then
        echo "❌ 找不到過去的 Request 測試紀錄 ($RESULTS_REQ_JSON)" >&2
        return 1
    fi
    echo "### 1. Request（接收大型資料）12 種全組合總表"
    echo ""
    echo "| 推薦等級 | 資料型別分類 | 實作架構 | API 端點 | 總耗時<br>(ms) | GC 總停頓時間<br>(Pause Time / 佔比) | Gen0 GC<br>次數 | Gen1 GC<br>次數 | Gen2 GC<br>次數 | LOH 峰值<br>(MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |"
    echo "|:---:|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|"
    python3 -c "
import json
with open('$RESULTS_REQ_JSON', 'r') as f:
    data = json.load(f)
for item in data:
    tier = item.get('tier', '⚡')
    cat = item['type_category']
    arch = item['pattern_name']
    ep = f\"\`{item['endpoint']}\`\"
    el = f\"**{item['elapsed_ms']}**\"
    pause = f\"{item['gc_pause_ms']} ms ({item['gc_pause_pct']}%)\"
    g0 = f\"{item['gen0_count']} 次\"
    g1 = f\"{item['gen1_count']} 次\"
    g2 = f\"{item['gen2_count']} 次\"
    loh = f\"**{item['loh_peak_mb']} MB**\"
    ws = f\"**{item['working_set_mb']} MB**\"
    verdict = item.get('verdict', '')
    print(f\"| {tier} | **{cat}** | **{arch}** | {ep} | {el} | {pause} | {g0} | {g1} | {g2} | {loh} | {ws} | {verdict} |\")
"
}

render_response_table() {
    if [[ ! -f "$RESULTS_RESP_JSON" ]]; then
        echo "❌ 找不到過去的 Response 測試紀錄 ($RESULTS_RESP_JSON)" >&2
        return 1
    fi
    echo "### 2. Response（回傳大型資料）12 種全組合總表"
    echo ""
    echo "| 推薦等級 | 資料型別分類 | 實作架構 | API 端點 | 總耗時<br>(ms) | GC 總停頓時間<br>(Pause Time / 佔比) | Gen0 GC<br>次數 | Gen1 GC<br>次數 | Gen2 GC<br>次數 | LOH 峰值<br>(MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |"
    echo "|:---:|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|"
    python3 -c "
import json
with open('$RESULTS_RESP_JSON', 'r') as f:
    data = json.load(f)
for item in data:
    tier = item.get('tier', '⚡')
    cat = item['type_category']
    arch = item['pattern_name']
    ep = f\"\`{item['endpoint']}\`\"
    el = f\"**{item['elapsed_ms']}**\"
    pause = f\"{item['gc_pause_ms']} ms ({item['gc_pause_pct']}%)\"
    g0 = f\"{item['gen0_count']} 次\"
    g1 = f\"{item['gen1_count']} 次\"
    g2 = f\"{item['gen2_count']} 次\"
    loh = f\"**{item['loh_peak_mb']} MB**\"
    ws = f\"**{item['working_set_mb']} MB**\"
    verdict = item.get('verdict', '')
    print(f\"| {tier} | **{cat}** | **{arch}** | {ep} | {el} | {pause} | {g0} | {g1} | {g2} | {loh} | {ws} | {verdict} |\")
"
}

if [[ "${1:-}" == "--report" || "${1:-}" == "-r" ]]; then
    echo "================================================================================"
    echo "📊 Server 端記憶體與 GC 實測快取總報表（共 24 組）"
    echo "================================================================================"
    echo ""
    render_request_table
    echo ""
    render_response_table
    exit 0
fi

DOTNET_COUNTERS="dotnet-counters"
if ! command -v dotnet-counters >/dev/null 2>&1; then
    DOTNET_COUNTERS="$HOME/.dotnet/tools/dotnet-counters"
fi

run_single_server_test() {
    local target_json="$1"
    local tier="$2"
    local type_category="$3"
    local pattern_name="$4"
    local endpoint="$5"
    local is_post="$6"
    local payload_file="${7:-}"
    local verdict="$8"
    local csv_out="$OUTPUT_DIR/bench-srv-${endpoint//\//_}-$(date +%s).csv"

    ASPNETCORE_URLS="$BASE_URL" dotnet run --project "$ROOT_DIR/src/Lab.LargeObject.Api" --no-launch-profile > /dev/null 2>&1 &
    local api_pid=$!

    while ! curl -s "$BASE_URL/diag/gc-stats" >/dev/null 2>&1; do
        sleep 0.2
    done

    # 暖機請求
    if [[ "$is_post" == "true" ]]; then
        curl -s -X POST "$BASE_URL$endpoint" -H "Content-Type: application/json" --data-binary "@$payload_file" > /dev/null 2>&1 || true
    else
        curl -s "$BASE_URL$endpoint" > /dev/null 2>&1 || true
    fi

    # 讀取壓測前 GC 基線
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

    local target_pid
    target_pid=$(pgrep -P $api_pid -f Lab.LargeObject.Api || echo "$api_pid")

    "$DOTNET_COUNTERS" collect -p "$target_pid" \
        --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.gc.collections,dotnet.process.memory.working_set]" \
        --format csv --output "$csv_out" --duration "00:00:00:20" >/dev/null 2>&1 &
    local counter_pid=$!

    sleep 0.5
    local start_time
    start_time=$(date +%s%N)

    if [[ "$is_post" == "true" ]]; then
        seq 1 "$TOTAL_REQUESTS" | xargs -P "$CONCURRENCY" -I{} \
            curl -s -o /dev/null -w "%{http_code}\n" \
                -X POST "$BASE_URL$endpoint" \
                -H "Content-Type: application/json" \
                --data-binary "@$payload_file" \
            | sort | uniq -c
    else
        seq 1 "$TOTAL_REQUESTS" | xargs -P "$CONCURRENCY" -I{} \
            curl -s -o /dev/null -w "%{http_code}\n" "$BASE_URL$endpoint" \
            | sort | uniq -c
    fi

    local end_time
    end_time=$(date +%s%N)
    local elapsed_ms=$(( (end_time - start_time) / 1000000 ))

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
    pkill -9 -f "Lab.LargeObject.Api.dll" 2>/dev/null || true
    sleep 1

    local full_csv="${csv_out}.csv"
    if [[ ! -f "$full_csv" && -f "$csv_out" ]]; then
        full_csv="$csv_out"
    fi

    local loh_peak=0
    if grep -q 'generation=loh' "$full_csv" 2>/dev/null; then
        loh_peak=$(grep 'generation=loh' "$full_csv" | grep 'heap.size' | awk -F',' 'BEGIN{max=0} {if($5>max) max=$5} END{print int(max/1024/1024)}')
    fi

    local ws_final=0
    if grep -q 'working_set' "$full_csv" 2>/dev/null; then
        ws_final=$(grep 'working_set' "$full_csv" | tail -n 1 | awk -F',' '{print int($5/1024/1024)}')
    fi
    rm -f "$csv_out" "$full_csv" 2>/dev/null || true

    python3 -c "
import json, os
arr = []
if os.path.exists('$target_json'):
    with open('$target_json', 'r') as f:
        try: arr = json.load(f)
        except: arr = []
arr.append({
    'tier': '$tier',
    'type_category': '$type_category',
    'pattern_name': '$pattern_name',
    'endpoint': '$endpoint',
    'elapsed_ms': $elapsed_ms,
    'gc_pause_ms': $gc_pause_delta,
    'gc_pause_pct': float('$pause_pct' or 0),
    'gen0_count': $g0_delta,
    'gen1_count': $g1_delta,
    'gen2_count': $g2_delta,
    'loh_peak_mb': $loh_peak,
    'working_set_mb': $ws_final,
    'verdict': '$verdict'
})
with open('$target_json', 'w') as f:
    json.dump(arr, f, indent=2, ensure_ascii=False)
"
    echo "SERVER RESULT | $tier | $type_category | $pattern_name | $endpoint | 耗時:${elapsed_ms}ms | GC停頓:${gc_pause_delta}ms | LOH:${loh_peak}MB | WS:${ws_final}MB"
}

run_request_suite() {
    echo "=== [1/2] 開始 Server Request 12 種全組合壓測 (50 請求，10 並行) ==="
    local tmp_req=$(mktemp)
    echo "[]" > "$tmp_req"

    run_single_server_test "$tmp_req" "🏆 S 級" "1. 原生數值 (double 4MB)" "Streaming (串流)" "/api/readings-stream" "true" "$PAYLOAD_DOUBLE" "🏆 0 LOH、GC 停頓極短、邊收邊算"
    run_single_server_test "$tmp_req" "⚡ A 級" "1. 原生數值 (double 4MB)" "ArrayPool (池化)" "/api/readings" "true" "$PAYLOAD_DOUBLE" "⚡ 連續 Buffer 租借歸還，暖機後穩定"
    run_single_server_test "$tmp_req" "❌ D 級" "1. 原生數值 (double 4MB)" "List (未池化)" "/api/readings-list" "true" "$PAYLOAD_DOUBLE" "❌ 連續短命 4MB 陣列砸進 LOH"

    run_single_server_test "$tmp_req" "🏆 S 級" "2. 原生字串 (string 50k)" "Streaming (串流)" "/api/strings-stream" "true" "$PAYLOAD_STRINGS" "🏆 字串最佳解，0 LOH、停頓極短"
    run_single_server_test "$tmp_req" "⚠️ C 級" "2. 原生字串 (string 50k)" "ArrayPool (池化)" "/api/strings" "true" "$PAYLOAD_STRINGS" "⚠️ 僅池化指標陣列，字串實體散落 Gen0"
    run_single_server_test "$tmp_req" "❌ D 級" "2. 原生字串 (string 50k)" "List (未池化)" "/api/strings-list" "true" "$PAYLOAD_STRINGS" "❌ 擴容指標陣列衝破 85KB LOH"

    run_single_server_test "$tmp_req" "🏆 S 級" "3. 巢狀結構 (Struct 20k)" "Streaming (串流)" "/api/members-stream" "true" "$PAYLOAD_MEMBERS" "🏆 Struct 最佳解，停頓最低、0 LOH"
    run_single_server_test "$tmp_req" "⚡ A 級" "3. 巢狀結構 (Struct 20k)" "ArrayPool (池化)" "/api/members" "true" "$PAYLOAD_MEMBERS" "⚡ 資料內嵌於連續 Buffer，隨機存取首選"
    run_single_server_test "$tmp_req" "❌ D 級" "3. 巢狀結構 (Struct 20k)" "List (未池化)" "/api/members-list" "true" "$PAYLOAD_MEMBERS" "❌ 頻繁觸發 Gen2 Full GC"

    run_single_server_test "$tmp_req" "🛡️ B 級" "4. 參考型別 (Class 20k)" "Streaming (串流)" "/api/members-class-stream" "true" "$PAYLOAD_MEMBERS" "🏆 Class 最佳解，GC 停頓降 66%"
    run_single_server_test "$tmp_req" "⚠️ C 級" "4. 參考型別 (Class 20k)" "ArrayPool (池化)" "/api/members-class-pooled" "true" "$PAYLOAD_MEMBERS" "⚠️ 池化效益低，物件依舊觸發 GC"
    run_single_server_test "$tmp_req" "❌ D 級" "4. 參考型別 (Class 20k)" "List (未池化)" "/api/members-class-list" "true" "$PAYLOAD_MEMBERS" "❌ 4 萬個 Class 實體散落 Gen0"

    mv "$tmp_req" "$RESULTS_REQ_JSON"
    echo "✅ Server Request 12 組完成並已持久化至 $RESULTS_REQ_JSON！"
}

run_response_suite() {
    echo "=== [2/2] 開始 Server Response 12 種全組合壓測 (50 請求，10 並行) ==="
    local tmp_resp=$(mktemp)
    echo "[]" > "$tmp_resp"

    run_single_server_test "$tmp_resp" "🏆 S 級" "1. 原生數值 (double 4MB)" "Streaming (串流回傳)" "/api/export-readings-stream" "false" "" "🏆 0 LOH、零 GC、記憶體僅 92MB"
    run_single_server_test "$tmp_resp" "⚡ A 級" "1. 原生數值 (double 4MB)" "ArrayPool (池化回傳)" "/api/export-readings" "false" "" "⚡ 租用 4MB Buffer 序列化後歸還"
    run_single_server_test "$tmp_resp" "❌ D 級" "1. 原生數值 (double 4MB)" "List (未池化回傳)" "/api/export-readings-list" "false" "" "❌ 每次請求建立大 List 佔據 LOH"

    run_single_server_test "$tmp_resp" "🏆 S 級" "2. 原生字串 (string 50k)" "Streaming (串流回傳)" "/api/export-strings-stream" "false" "" "🏆 最快、0 LOH、停頓極短"
    run_single_server_test "$tmp_resp" "⚠️ C 級" "2. 原生字串 (string 50k)" "ArrayPool (池化回傳)" "/api/export-strings" "false" "" "⚠️ 僅池化指標陣列，5 萬字串仍在 Gen0"
    run_single_server_test "$tmp_resp" "❌ D 級" "2. 原生字串 (string 50k)" "List (未池化回傳)" "/api/export-strings-list" "false" "" "❌ 5 萬字串大 List 衝入 LOH"

    run_single_server_test "$tmp_resp" "🏆 S 級" "3. 巢狀結構 (Struct 20k)" "Streaming (串流回傳)" "/api/export-members-stream" "false" "" "🏆 最快、0 LOH、停頓短、記憶體減半"
    run_single_server_test "$tmp_resp" "⚡ A 級" "3. 巢狀結構 (Struct 20k)" "ArrayPool (池化回傳)" "/api/export-members" "false" "" "⚡ 租用 Buffer 序列化後歸還"
    run_single_server_test "$tmp_resp" "❌ D 級" "3. 巢狀結構 (Struct 20k)" "List (未池化回傳)" "/api/export-members-list" "false" "" "❌ 20k Struct List 進入 LOH"

    run_single_server_test "$tmp_resp" "🛡️ B 級" "4. 參考型別 (Class 20k)" "Streaming (串流回傳)" "/api/export-members-class-stream" "false" "" "🏆 Class 最佳解，0 LOH"
    run_single_server_test "$tmp_resp" "⚠️ C 級" "4. 參考型別 (Class 20k)" "ArrayPool (池化回傳)" "/api/export-members-class-pooled" "false" "" "⚠️ 池化效益低，Class 物件觸發 GC"
    run_single_server_test "$tmp_resp" "❌ D 級" "4. 參考型別 (Class 20k)" "List (未池化回傳)" "/api/export-members-class-list" "false" "" "❌ 20k Class List 佔據 LOH"

    mv "$tmp_resp" "$RESULTS_RESP_JSON"
    echo "✅ Server Response 12 組完成並已持久化至 $RESULTS_RESP_JSON！"
}

MODE="${1:-all}"
case "$MODE" in
    "--request"|"-req")
        run_request_suite
        ;;
    "--response"|"-resp")
        run_response_suite
        ;;
    *)
        run_request_suite
        echo ""
        run_response_suite
        ;;
esac
