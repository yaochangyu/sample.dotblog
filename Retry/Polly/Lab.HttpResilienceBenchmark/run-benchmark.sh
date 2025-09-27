#!/bin/bash

echo "=== HTTP Resilience vs Polly 效能比較測試 ==="
echo ""

# 檢查 Web API 是否運行
echo "檢查 Web API 是否在 http://localhost:5000 運行..."
if curl -s http://localhost:5000/api/members >/dev/null 2>&1; then
    echo "✓ Web API 正在運行"
else
    echo "❌ Web API 未運行，請先啟動："
    echo "   cd src/HttpResilienceBenchmark.Api && dotnet run"
    exit 1
fi

echo ""
echo "開始運行效能測試..."
cd src/HttpResilienceBenchmark.Console
dotnet run -c Release