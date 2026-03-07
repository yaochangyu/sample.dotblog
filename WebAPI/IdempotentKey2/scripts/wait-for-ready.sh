#!/usr/bin/env bash
set -euo pipefail

wait_for() {
  local name="$1"
  local url="$2"
  local max=30

  echo "Waiting for ${name} to be ready..."
  for i in $(seq 1 $max); do
    if curl -sf "$url" > /dev/null 2>&1; then
      echo "${name} is ready."
      return 0
    fi
    echo "  attempt $i/$max ..."
    sleep 2
  done

  echo "${name} did not become ready in time." >&2
  return 1
}

wait_for "Pod1 (8080)" "http://localhost:8080/weatherforecast"
wait_for "Pod2 (8081)" "http://localhost:8081/weatherforecast"
