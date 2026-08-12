#!/usr/bin/env bash
set -euo pipefail

# 用官方 dotnet-counters 觀察 GC/LOH 相關計數器（取代自建 /diag/gc + curl 輪詢）。
# 需要先安裝：dotnet tool install -g dotnet-counters
# 用法：./observe-counters.sh <pid-or-process-name> [duration-seconds]
# 範例（用 pid）：      ./observe-counters.sh 12345 60
# 範例（用 process 名稱）：./observe-counters.sh Lab.LargeObject.Api 60

TARGET="${1:?請提供 process id 或 process 名稱，例如 12345 或 Lab.LargeObject.Api}"
DURATION_SECONDS="${2:-60}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_FILE="$SCRIPT_DIR/counters-$(date +%Y%m%d-%H%M%S)"

DOTNET_COUNTERS="dotnet-counters"
if ! command -v dotnet-counters >/dev/null 2>&1; then
    if [[ -x "$HOME/.dotnet/tools/dotnet-counters" ]]; then
        DOTNET_COUNTERS="$HOME/.dotnet/tools/dotnet-counters"
    else
        echo "找不到 dotnet-counters，先安裝：dotnet tool install -g dotnet-counters"
        exit 1
    fi
fi

# --duration 格式是 dd:hh:mm:ss
DURATION_HMS=$(printf '00:00:%02d:%02d' $((DURATION_SECONDS / 60)) $((DURATION_SECONDS % 60)))

if [[ "$TARGET" =~ ^[0-9]+$ ]]; then
    TARGET_ARGS=(-p "$TARGET")
else
    TARGET_ARGS=(-n "$TARGET")
fi

echo "觀察目標：$TARGET，時長 ${DURATION_SECONDS}s"
echo "輸出檔：${OUT_FILE}.csv"
echo "----"

"$DOTNET_COUNTERS" collect \
    "${TARGET_ARGS[@]}" \
    --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.gc.last_collection.heap.fragmentation.size,dotnet.gc.collections,dotnet.gc.heap.total_allocated,dotnet.process.memory.working_set]" \
    --format csv \
    --output "$OUT_FILE" \
    --duration "$DURATION_HMS"

echo "----"
echo "觀察結束，CSV 已寫入：${OUT_FILE}.csv"
echo "只看 LOH 大小變化："
echo "  grep 'generation=loh' \"${OUT_FILE}.csv\" | grep 'heap.size'"
