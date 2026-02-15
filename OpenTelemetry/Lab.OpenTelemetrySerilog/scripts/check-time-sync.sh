#!/bin/bash
echo "=== 並行檢查容器時間（整齊輸出）==="

# 並行執行
docker exec frontend date +"%Y-%m-%d %H:%M:%S %Z" > /tmp/t1.txt 2>/dev/null &
docker exec backend-a date +"%Y-%m-%d %H:%M:%S %Z" > /tmp/t2.txt 2>/dev/null &
docker exec backend-b date +"%Y-%m-%d %H:%M:%S %Z" > /tmp/t3.txt 2>/dev/null &
docker exec jaeger date +"%Y-%m-%d %H:%M:%S %Z" > /tmp/t4.txt 2>/dev/null &
wait

# 整齊輸出
printf "%-12s %s\n" "Frontend:" "$(cat /tmp/t1.txt)"
printf "%-12s %s\n" "Backend-A:" "$(cat /tmp/t2.txt)"
printf "%-12s %s\n" "Backend-B:" "$(cat /tmp/t3.txt)"
printf "%-12s %s\n" "Jaeger:" "$(cat /tmp/t4.txt)"
printf "%-12s %s\n" "宿主機:" "$(date +"%Y-%m-%d %H:%M:%S %Z")"

# 檢查一致性
T1=$(cat /tmp/t1.txt | cut -d' ' -f2)
T2=$(cat /tmp/t2.txt | cut -d' ' -f2)
T3=$(cat /tmp/t3.txt | cut -d' ' -f2)
T4=$(cat /tmp/t4.txt | cut -d' ' -f2)

echo ""
if [ "$T1" = "$T2" ] && [ "$T2" = "$T3" ] && [ "$T3" = "$T4" ]; then
  echo "✅ 所有容器時間完全同步！"
else
  echo "⚠️  偵測到時間差異"
fi