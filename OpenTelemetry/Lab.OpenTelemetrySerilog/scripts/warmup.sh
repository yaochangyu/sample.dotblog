#!/bin/sh

warmup() {
  name=$1; url=$2; retries=0; max=30
  echo "Warming up $name..."
  until curl -sf -o /dev/null "$url"; do
    retries=$((retries + 1))
    if [ $retries -ge $max ]; then
      echo "$name FAILED (after $max retries)"
      return 1
    fi
    echo "$name not ready, retrying ($retries/$max)..."
    sleep 2
  done
  echo "$name OK"
}

warmup "backend-b" "http://backend-b:8080/Weather"
warmup "backend-a" "http://backend-a:8080/Weather"
warmup "frontend"  "http://frontend:3000/api/weather"
echo "Warmup complete!"
