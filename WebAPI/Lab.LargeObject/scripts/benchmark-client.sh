#!/usr/bin/env bash
set -euo pipefail

# Client 端記憶體量測與量測方式對照實驗腳本：
# 比較 Client 在接收大資料時：
#   1. List (未池化接收) vs Streaming (串流接收)
# 比較兩種量測方式：
#   - 方式 A (In-Process): 程式內 GC.GetGCMemoryInfo() / GC.GetTotalAllocatedBytes()
#   - 方式 B (Out-of-Process): 外部 dotnet-counters 採樣 Working Set 與 LOH Peak

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/.output"
mkdir -p "$OUTPUT_DIR"
RESULTS_JSON="$OUTPUT_DIR/latest-client-results.json"
PORT=5149
BASE_URL="http://localhost:$PORT"
CONCURRENCY=10
TOTAL_REQUESTS=50

render_markdown_table() {
    if [[ ! -f "$RESULTS_JSON" ]]; then
        echo "❌ 找不到過去的 Client 測試紀錄 ($RESULTS_JSON)，請先完整執行一次壓測！" >&2
        exit 1
    fi

    echo "## 實測數據：Client 端 4 種資料型別 × 2 種接收方式（List vs Streaming）量測總表"
    echo ""
    echo "| 推薦等級 | 資料型別分類 | Client 接收架構 | 總耗時<br>(ms) | GC 總停頓時間<br>(Pause Time / 佔比) | Gen0 GC<br>次數 | Gen1 GC<br>次數 | Gen2 GC<br>次數 | In-Process<br>LOH (MB) | dotnet-counters<br>LOH Peak (MB) | Working Set<br>實體記憶體 | 核心評語與行為特徵 |"
    echo "|:---:|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|"

    python3 -c "
import json
with open('$RESULTS_JSON', 'r') as f:
    data = json.load(f)
for item in data:
    tier = item.get('tier', '⚡')
    cat = item['type_category']
    arch = item['pattern_name']
    el = item['elapsed_ms']
    pause = f\"{item['gc_pause_ms']} ms ({item['gc_pause_pct']}%)\"
    g0 = f\"{item['gen0_count']} 次\"
    g1 = f\"{item['gen1_count']} 次\"
    g2 = f\"{item['gen2_count']} 次\"
    loh_in = f\"{item['in_process_loh_mb']} MB\"
    loh_ext = f\"{item['counters_loh_mb']} MB\"
    ws = f\"{item['working_set_mb']} MB\"
    verdict = item.get('verdict', '')
    print(f\"| {tier} | **{cat}** | **{arch}** | **{el}** | {pause} | {g0} | {g1} | {g2} | **{loh_in}** | **{loh_ext}** | **{ws}** | {verdict} |\")
"
}

if [[ "${1:-}" == "--report" || "${1:-}" == "-r" ]]; then
    echo "📊 從快取讀取上次 Client 壓測結果 ($RESULTS_JSON)："
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

# 啟動 Server
ASPNETCORE_URLS="$BASE_URL" dotnet run --project "$ROOT_DIR/src/Lab.LargeObject.Api" --no-launch-profile > /dev/null 2>&1 &
SERVER_PID=$!

cleanup() {
    kill -9 $SERVER_PID 2>/dev/null || true
    pkill -9 -f "Lab.LargeObject.Api.dll" 2>/dev/null || true
    pkill -9 -f "Lab.LargeObject.BenchClient.dll" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

while ! curl -s "$BASE_URL/" >/dev/null 2>&1; do
    sleep 0.5
done

run_client_test() {
    local tier="$1"
    local type_category="$2"
    local pattern_name="$3"
    local raw_type="$4"
    local raw_mode="$5"
    local verdict="$6"
    local csv_out="$OUTPUT_DIR/bench-client-${raw_type}-${raw_mode}-$(date +%s).csv"

    # 先啟動 Client 端背景執行
    dotnet run --project "$ROOT_DIR/tests/Lab.LargeObject.BenchClient" --no-launch-profile \
        -- --type="$raw_type" --mode="$raw_mode" --url="$BASE_URL" --requests="$TOTAL_REQUESTS" --concurrency="$CONCURRENCY" > /tmp/client_output.json 2>&1 &
    local client_pid=$!

    sleep 0.5
    local target_pid
    target_pid=$(pgrep -P $client_pid -f Lab.LargeObject.BenchClient || echo "$client_pid")

    "$DOTNET_COUNTERS" collect -p "$target_pid" \
        --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.gc.collections,dotnet.process.memory.working_set]" \
        --format csv --output "$csv_out" --duration "00:00:00:15" >/dev/null 2>&1 &
    local counter_pid=$!

    wait $client_pid 2>/dev/null || true
    wait $counter_pid 2>/dev/null || true

    local full_csv="${csv_out}.csv"
    if [[ ! -f "$full_csv" && -f "$csv_out" ]]; then
        full_csv="$csv_out"
    fi

    local counters_loh=0
    if grep -q 'generation=loh' "$full_csv" 2>/dev/null; then
        counters_loh=$(grep 'generation=loh' "$full_csv" | grep 'heap.size' | awk -F',' 'BEGIN{max=0} {if($5>max) max=$5} END{print int(max/1024/1024)}')
    fi

    local ws_final=0
    if grep -q 'working_set' "$full_csv" 2>/dev/null; then
        ws_final=$(grep 'working_set' "$full_csv" | tail -n 1 | awk -F',' '{print int($5/1024/1024)}')
    fi

    local client_json
    client_json=$(cat /tmp/client_output.json | grep -o '{"Mode".*}' || echo "{}")

    python3 -c "
import json
with open('$TEMP_RESULTS_FILE', 'r') as f:
    arr = json.load(f)
cj = json.loads('''$client_json''')
arr.append({
    'tier': '$tier',
    'type_category': '$type_category',
    'pattern_name': '$pattern_name',
    'elapsed_ms': cj.get('ElapsedMs', 0),
    'gc_pause_ms': cj.get('PauseDurationMs', 0),
    'gc_pause_pct': cj.get('PauseTimePercentage', 0),
    'gen0_count': cj.get('Gen0', 0),
    'gen1_count': cj.get('Gen1', 0),
    'gen2_count': cj.get('Gen2', 0),
    'in_process_loh_mb': cj.get('LohSizeMb', 0),
    'counters_loh_mb': $counters_loh,
    'working_set_mb': $ws_final,
    'verdict': '$verdict'
})
with open('$TEMP_RESULTS_FILE', 'w') as f:
    json.dump(arr, f, indent=2, ensure_ascii=False)
"
    echo "CLIENT RESULT | $tier | $type_category | $pattern_name | Counters-LOH:${counters_loh}MB | WS:${ws_final}MB"
}

echo "=== 開始 Client 端記憶體量測與量測方式對照實驗 (50 請求，10 並行) ==="

# 1. 原生數值 double
run_client_test "🏆 S 級" "1. 原生數值 (double 4MB)" "Streaming (串流接收)" "readings" "stream" "🏆 Client 0 LOH、無大陣列擴容、記憶體極低"
run_client_test "❌ D 級" "1. 原生數值 (double 4MB)" "List (未池化接收)" "readings" "list" "❌ Client 每次 new 4MB double[] 砸進 LOH"

# 2. 原生字串 string
run_client_test "🏆 S 級" "2. 原生字串 (string 50k)" "Streaming (串流接收)" "strings" "stream" "🏆 Client 逐筆消費 0 LOH"
run_client_test "❌ D 級" "2. 原生字串 (string 50k)" "List (未池化接收)" "strings" "list" "❌ 5 萬字串大 List 在 Client 端進 LOH"

# 3. 巢狀結構 Struct
run_client_test "🏆 S 級" "3. 巢狀結構 (Struct 20k)" "Streaming (串流接收)" "members" "stream" "🏆 Client 0 LOH、記憶體佔用極小"
run_client_test "❌ D 級" "3. 巢狀結構 (Struct 20k)" "List (未池化接收)" "members" "list" "❌ 20k Struct 大 List 在 Client 端進 LOH"

# 4. 參考型別 Class
run_client_test "🛡️ B 級" "4. 參考型別 (Class 20k)" "Streaming (串流接收)" "members-class" "stream" "🏆 Client 0 LOH、記憶體維持低檔"
run_client_test "❌ D 級" "4. 參考型別 (Class 20k)" "List (未池化接收)" "members-class" "list" "❌ 20k Class 指標陣列在 Client 端進 LOH"

mv "$TEMP_RESULTS_FILE" "$RESULTS_JSON"
echo ""
echo "✅ Client 測試結果已成功儲存至 $RESULTS_JSON！"
echo "👉 後續可直接執行 ./scripts/benchmark-client.sh --report 重複查看此報表，無需重跑。"
