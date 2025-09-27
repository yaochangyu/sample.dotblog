#!/bin/bash

echo "=== 啟動 Web API 伺服器 ==="
echo "API 將在 http://localhost:5000 運行"
echo "端點: http://localhost:5000/api/members"
echo ""
echo "按 Ctrl+C 停止伺服器"
echo ""

cd src/HttpResilienceBenchmark.Api
dotnet run