#!/usr/bin/env bash
set -euo pipefail

# 全套壓測一鍵總指揮腳本（涵蓋 Server 24組 + Client 8組 = 共 32 組全場景）
# 參數支援：
#   ./benchmark-all.sh           # 一鍵完整重跑 Server 與 Client 全套 32 組壓測並持久化
#   ./benchmark-all.sh --report  # 一鍵輸出 Server 與 Client 的 Markdown 彙總大表（0.1 秒秒級重用）

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ "${1:-}" == "--report" || "${1:-}" == "-r" ]]; then
    echo "================================================================================"
    echo "📊 全套 32 組壓測結果快取總報表（Server 24組 + Client 8組）"
    echo "================================================================================"
    echo ""
    "$SCRIPT_DIR/benchmark-server.sh" --report
    echo ""
    echo "--------------------------------------------------------------------------------"
    echo ""
    "$SCRIPT_DIR/benchmark-client.sh" --report
    exit 0
fi

echo "================================================================================"
echo "🚀 開始執行全套 32 組完整壓測（Server 24組 + Client 8組）"
echo "================================================================================"
echo ""

echo ">>> [1/2] 正在執行 Server 端 24 種全組合壓測（Request 12組 + Response 12組）..."
"$SCRIPT_DIR/benchmark-server.sh"
echo "✅ Server 端 24 組完成！"
echo ""

echo ">>> [2/2] 正在執行 Client 端 8 組實測與量測方式對照..."
"$SCRIPT_DIR/benchmark-client.sh"
echo "✅ Client 端 8 組完成！"
echo ""

echo "================================================================================"
echo "🎉 全套 32 組壓測全數順利完成！"
echo "👉 後續可隨時執行 ./scripts/benchmark-all.sh --report 查看完整大表"
echo "================================================================================"
