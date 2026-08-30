#!/usr/bin/env bash
set -euo pipefail

# 全套壓測一鍵總指揮腳本（涵蓋 Request 12組 + Response 12組 + Client 8組 = 共 32 組全場景）
# 參數支援：
#   ./benchmark-all.sh           # 一鍵完整重跑全部 3 大壓測套件並持久化
#   ./benchmark-all.sh --report  # 一鍵輸出全部 3 大套件的 Markdown 彙總大表（0.1 秒秒級重用）

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ "${1:-}" == "--report" || "${1:-}" == "-r" ]]; then
    echo "================================================================================"
    echo "📊 全套 32 組壓測結果快取總報表"
    echo "================================================================================"
    echo ""
    "$SCRIPT_DIR/benchmark-request.sh" --report
    echo ""
    echo "--------------------------------------------------------------------------------"
    echo ""
    "$SCRIPT_DIR/benchmark-response.sh" --report
    echo ""
    echo "--------------------------------------------------------------------------------"
    echo ""
    "$SCRIPT_DIR/benchmark-client.sh" --report
    exit 0
fi

echo "================================================================================"
echo "🚀 開始執行全套 32 組完整壓測（Request 12組 + Response 12組 + Client 8組）"
echo "================================================================================"
echo ""

echo ">>> [1/3] 正在執行 Request 12 種全組合壓測..."
"$SCRIPT_DIR/benchmark-request.sh"
echo "✅ Request 12 組完成！"
echo ""

echo ">>> [2/3] 正在執行 Response 12 種全組合壓測..."
"$SCRIPT_DIR/benchmark-response.sh"
echo "✅ Response 12 組完成！"
echo ""

echo ">>> [3/3] 正在執行 Client 端 8 組實測與量測方式對照..."
"$SCRIPT_DIR/benchmark-client.sh"
echo "✅ Client 8 組完成！"
echo ""

echo "================================================================================"
echo "🎉 全套 32 組壓測全數順利完成！"
echo "👉 後續可隨時執行 ./scripts/benchmark-all.sh --report 查看完整大表"
echo "================================================================================"
