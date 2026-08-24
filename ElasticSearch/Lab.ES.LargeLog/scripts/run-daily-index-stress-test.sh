#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.yml"
API_PROJECT="$ROOT_DIR/src/EsDailyLogsApi/EsDailyLogsApi.csproj"
K6_SCRIPT="$ROOT_DIR/scripts/k6-daily-index-logs.js"
RPS_LEVELS=(50 100 150)
K6_DURATION_RAW="${K6_DURATION:-}"
K6_DURATION="${K6_DURATION:-30s}"
RUN_ID="$(date +%Y%m%d-%H%M%S)"
RUN_DIR="$ROOT_DIR/.output/es-traditional-stress-test/$RUN_ID"
API_LOG="$RUN_DIR/api.log"
REPORT_FILE="$RUN_DIR/daily-index-stress-report.md"
ES_URL="http://127.0.0.1:9200"
TODAY_INDEX="logs-app-$(date -u +%Y.%m.%d)"
SUMMARY_ROWS_FILE="$RUN_DIR/summary-rows.md"
ES_ROWS_FILE="$RUN_DIR/es-rows.md"
RUN_TO_TARGET_DOCS="${RUN_TO_TARGET_DOCS:-0}"
TARGET_DOCS="${TARGET_DOCS:-10000000}"
TARGET_RPS="${TARGET_RPS:-5000}"
RUN_PRESET="${RUN_PRESET:-}"

if [[ "$RUN_PRESET" == "12h" ]]; then
  RUN_TO_TARGET_DOCS=1
  TARGET_DOCS=10000000
  TARGET_RPS=232
  K6_DURATION="12h"
fi

if [[ "$RUN_TO_TARGET_DOCS" == "1" ]]; then
  DURATION_SECONDS=$(((TARGET_DOCS + TARGET_RPS - 1) / TARGET_RPS))
  if [[ -z "$K6_DURATION_RAW" ]]; then
    K6_DURATION="${DURATION_SECONDS}s"
  fi
  RPS_LEVELS=("$TARGET_RPS")
fi

mkdir -p "$RUN_DIR"

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi

  docker compose -f "$COMPOSE_FILE" down --remove-orphans
}

trap cleanup EXIT

preflight_cleanup() {
  docker rm -f es-lab >/dev/null 2>&1 || true
}

wait_for_ok() {
  local url="$1"
  local attempt=0

  until curl -fsS "$url" >/dev/null; do
    attempt=$((attempt + 1))
    if (( attempt >= 60 )); then
      echo "timeout waiting for $url" >&2
      return 1
    fi
    sleep 2
  done
}

write_report_header() {
  : >"$SUMMARY_ROWS_FILE"
  : >"$ES_ROWS_FILE"
  cat >"$REPORT_FILE" <<EOF
# Daily index endpoint stress test report

- Target endpoint: POST /api/daily-index/logs
- API URL: http://127.0.0.1:5287/api/daily-index/logs
- RPS levels: ${RPS_LEVELS[*]}
- Duration per level: ${K6_DURATION}
- Run directory: ${RUN_DIR}
EOF

  if [[ "$RUN_TO_TARGET_DOCS" == "1" ]]; then
    cat >>"$REPORT_FILE" <<EOF
- Target docs: ${TARGET_DOCS}
- Target RPS: ${TARGET_RPS}

EOF
  fi

  if [[ -n "$RUN_PRESET" ]]; then
    cat >>"$REPORT_FILE" <<EOF
- Preset: ${RUN_PRESET}

EOF
  else
    cat >>"$REPORT_FILE" <<EOF

EOF
  fi

  cat >>"$REPORT_FILE" <<EOF

## Summary

| RPS | Requests | Failed checks | Avg latency (ms) | p95 latency (ms) | HTTP success rate |
|---|---:|---:|---:|---:|---:|
EOF
}

append_es_row() {
  local rps="$1"
  local count="$2"
  printf '| %s | %s |\n' "$rps" "$count" >>"$ES_ROWS_FILE"
}

count_written_docs() {
  curl -fsS -X POST "$ES_URL/$TODAY_INDEX/_refresh" >/dev/null
  curl -fsS -X POST "$ES_URL/$TODAY_INDEX/_count" \
    -H 'Content-Type: application/json' \
    -d '{"query":{"match_all":{}}}' | python3 -c 'import json,sys; print(json.load(sys.stdin)["count"])'
}

wait_for_es_count() {
  local expected_count="$1"
  local attempt=0
  local count=0

  if [[ "$expected_count" -gt 0 ]]; then
    until [[ "$count" -ge "$expected_count" ]]; do
      count="$(count_written_docs)"
      if [[ "$count" -ge "$expected_count" ]]; then
        printf '%s' "$count"
        return 0
      fi

      attempt=$((attempt + 1))
      if (( attempt >= 30 )); then
        printf '%s' "$count"
        return 0
      fi

      sleep 1
    done
  else
    until [[ "$count" -gt 0 ]]; do
      count="$(count_written_docs)"
      if [[ "$count" -gt 0 ]]; then
        printf '%s' "$count"
        return 0
      fi

      attempt=$((attempt + 1))
      if (( attempt >= 30 )); then
        printf '%s' "$count"
        return 0
      fi

      sleep 1
    done
  fi
}

append_report_row() {
  local rps="$1"
  local summary_json="$2"
  python3 - "$rps" "$summary_json" <<'PY'
import json
import sys

rps = sys.argv[1]
path = sys.argv[2]

with open(path, 'r', encoding='utf-8') as handle:
    data = json.load(handle)

metrics = data.get('metrics', {})
status_check = data.get('root_group', {}).get('checks', {}).get('status is 201', {})

http_reqs = metrics.get('http_reqs', {})
duration = metrics.get('http_req_duration', {})

requests = http_reqs.get('count', 0)
avg = duration.get('avg', 0)
p95 = duration.get('p(95)', duration.get('p95', 0))
passes = status_check.get('passes', 0)
fails = status_check.get('fails', 0)
success_rate = 0 if passes + fails == 0 else round((passes / (passes + fails)) * 100, 2)

print(f'| {rps} | {requests:.0f} | {fails:.0f} | {avg:.2f} | {p95:.2f} | {success_rate}% |')
PY
}

finalize_report() {
  {
    cat "$SUMMARY_ROWS_FILE"
    cat <<'EOF'

## ES Verification

| RPS | ES documents with this run tag |
|---|---:|
EOF
    cat "$ES_ROWS_FILE"
    cat <<'EOF'

## Notes

- 這份報告壓的是手動按日索引端點，不是 Data Stream。
- 201 代表寫入成功，檢查條件以 HTTP status 為準。
EOF
  } >>"$REPORT_FILE"
}

echo "[1/4] 啟動 Elasticsearch"
preflight_cleanup
docker compose -f "$COMPOSE_FILE" up -d
wait_for_ok "http://localhost:9200/_cluster/health"

echo "[2/4] 啟動 Web API"
dotnet run --project "$API_PROJECT" --launch-profile http >"$API_LOG" 2>&1 &
API_PID=$!
wait_for_ok "http://localhost:5287/api/daily-index/logs?size=1"

echo "[3/4] 開始 k6 壓測"
write_report_header
for rps in "${RPS_LEVELS[@]}"; do
  echo "--- RPS $rps / duration $K6_DURATION ---"
  SUMMARY_JSON="$RUN_DIR/summary-${rps}.json"
  docker run --rm --network host \
    -u "$(id -u):$(id -g)" \
    -e RPS="$rps" \
    -e DURATION="$K6_DURATION" \
    -e API_URL="http://127.0.0.1:5287/api/daily-index/logs" \
    -e RUN_ID="$RUN_ID" \
    -v "$K6_SCRIPT:/scripts/k6-traditional-logs.js:ro" \
    -v "$RUN_DIR:/reports" \
    grafana/k6:0.56.0 run --summary-export "/reports/summary-${rps}.json" /scripts/k6-traditional-logs.js
  append_report_row "$rps" "$SUMMARY_JSON" >>"$SUMMARY_ROWS_FILE"
  if [[ "$RUN_TO_TARGET_DOCS" == "1" ]]; then
    ES_COUNT="$(wait_for_es_count "$TARGET_DOCS")"
  else
    ES_COUNT="$(wait_for_es_count 0)"
  fi
  append_es_row "$rps" "$ES_COUNT"
done
finalize_report

echo "[4/4] 完成"
echo "Report: $REPORT_FILE"
