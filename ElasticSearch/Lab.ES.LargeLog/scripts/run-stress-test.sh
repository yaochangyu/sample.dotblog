#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.yml"
API_PROJECT="$ROOT_DIR/src/EsDailyLogsApi/EsDailyLogsApi.csproj"
K6_SCRIPT="$ROOT_DIR/scripts/k6-write-es.js"
RPS_LEVELS=(50 100 150)
K6_DURATION="${K6_DURATION:-60s}"
RUN_ID="$(date +%Y%m%d-%H%M%S)"
RUN_DIR="/tmp/opencode/es-stress-test/$RUN_ID"
API_LOG="$RUN_DIR/api.log"

mkdir -p "$RUN_DIR"

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi

  docker compose -f "$COMPOSE_FILE" down --remove-orphans
}

trap cleanup EXIT

wait_for_http() {
  local url="$1"
  local expected="$2"
  local attempt=0

  until curl -fsS "$url" | grep -Eq "$expected"; do
    attempt=$((attempt + 1))
    if (( attempt >= 60 )); then
      echo "timeout waiting for $url" >&2
      return 1
    fi
    sleep 2
  done
}

wait_for_status() {
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

echo "[1/4] 啟動 Elasticsearch"
docker compose -f "$COMPOSE_FILE" up -d
wait_for_http "http://localhost:9200/_cluster/health" '"status":"green"|"status":"yellow"'

echo "[2/4] 啟動 Web API"
dotnet run --project "$API_PROJECT" --launch-profile http >"$API_LOG" 2>&1 &
API_PID=$!
wait_for_status "http://localhost:5287/api/logs"

echo "[3/4] 開始 k6 壓測"
for rps in "${RPS_LEVELS[@]}"; do
  echo "--- RPS $rps / duration $K6_DURATION ---"
  docker run --rm --network host \
    -e RPS="$rps" \
    -e DURATION="$K6_DURATION" \
    -e API_URL="http://127.0.0.1:5287/api/logs" \
    -v "$K6_SCRIPT:/scripts/k6-write-es.js:ro" \
    grafana/k6:0.56.0 run /scripts/k6-write-es.js
done

echo "[4/4] 完成，輸出保留在 $RUN_DIR"
