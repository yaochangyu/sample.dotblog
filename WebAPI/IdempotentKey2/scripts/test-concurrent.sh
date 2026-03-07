#!/usr/bin/env bash
set -euo pipefail

KEY="concurrent-test-$(date +%s)"
echo "Testing concurrent requests with Idempotency-Key: $KEY"
echo "Pod1 → http://localhost:8080  |  Pod2 → http://localhost:8081"

# 兩個請求同時送往不同 pod，共享同一個 Redis 分散式鎖
curl -s -o /dev/null -w "%{http_code}" -X POST \
  "http://localhost:8080/weatherforecast/25" \
  -H "Idempotency-Key: $KEY" > /tmp/idempotent_code1.txt &
PID1=$!

curl -s -o /dev/null -w "%{http_code}" -X POST \
  "http://localhost:8081/weatherforecast/25" \
  -H "Idempotency-Key: $KEY" > /tmp/idempotent_code2.txt &
PID2=$!

wait $PID1 $PID2

CODE1=$(cat /tmp/idempotent_code1.txt)
CODE2=$(cat /tmp/idempotent_code2.txt)
echo "Pod1 (8080): HTTP $CODE1"
echo "Pod2 (8081): HTTP $CODE2"

if { [ "$CODE1" = "200" ] && [ "$CODE2" = "409" ]; } || \
   { [ "$CODE1" = "409" ] && [ "$CODE2" = "200" ]; }; then
  echo "✅ Concurrent test passed: one 200, one 409"
else
  echo "❌ Concurrent test failed: expected one 200 and one 409, got $CODE1 and $CODE2"
  exit 1
fi
