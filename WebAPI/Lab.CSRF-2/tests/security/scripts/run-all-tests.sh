#!/bin/bash

# 執行所有安全性測試

echo "=========================================="
echo "  執行所有安全性測試"
echo "=========================================="
echo ""

API_BASE="${API_BASE:-http://localhost:5073}"
export API_BASE

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TOTAL=0
PASSED=0
FAILED=0

# 測試腳本列表
tests=(
    "test-01-normal-flow.sh|正常流程測試"
    "test-02-token-expiration.sh|Token 過期測試"
    "test-03-usage-limit.sh|使用次數限制測試"
    "test-04-missing-token.sh|無 Token 測試"
    "test-05-invalid-token.sh|無效 Token 測試"
    "test-06-ua-mismatch.sh|User-Agent 不一致測試"
    "test-07-rate-limiting.sh|速率限制測試"
    "test-11-direct-attack.sh|直接攻擊測試"
    "test-12-replay-attack.sh|重放攻擊測試"
)

echo "API 伺服器: $API_BASE"
echo "測試腳本數量: ${#tests[@]}"
echo ""
echo "=========================================="
echo ""

for test_info in "${tests[@]}"; do
    IFS='|' read -r script_name test_name <<< "$test_info"
    TOTAL=$((TOTAL + 1))
    
    echo "[$TOTAL/${#tests[@]}] 執行: $test_name"
    echo "----------------------------------------"
    
    if bash "$SCRIPT_DIR/$script_name"; then
        PASSED=$((PASSED + 1))
        echo ""
    else
        FAILED=$((FAILED + 1))
        echo ""
    fi
    
    # 測試之間暫停，避免速率限制影響
    if [ "$script_name" != "test-12-replay-attack.sh" ]; then
        sleep 2
    fi
done

echo "=========================================="
echo "  測試結果統計"
echo "=========================================="
echo "總測試數: $TOTAL"
echo "通過: $PASSED"
echo "失敗: $FAILED"
echo "=========================================="

if [ $FAILED -eq 0 ]; then
    echo ""
    echo "✅ 所有測試通過！"
    exit 0
else
    echo ""
    echo "❌ 有 $FAILED 個測試失敗"
    exit 1
fi
