# HTTP Resilience vs Polly 效能比較測試

這個專案比較 Microsoft.Extensions.Http.Resilience 和 Microsoft.Extensions.Http.Polly 兩個套件的效能。

## 專案結構

```
HttpResilienceBenchmark/
├── src/
│   ├── HttpResilienceBenchmark.Api/          # Web API 專案
│   └── HttpResilienceBenchmark.Console/      # 效能測試控制台專案
└── HttpResilienceBenchmark.sln
```

## 如何運行

### 方法一：使用腳本（推薦）

1. **啟動 Web API**
```bash
./start-api.sh
```

2. **運行效能測試**（在新的終端視窗）
```bash
./run-benchmark.sh
```

### 方法二：手動運行

1. **啟動 Web API**
```bash
cd src/HttpResilienceBenchmark.Api
dotnet run
```

API 會在 http://localhost:5000 上運行，提供 `/api/members` 端點，回傳：
```json
{"name":"小章","age":18}
```

2. **運行效能測試**（在新的終端視窗）
```bash
cd src/HttpResilienceBenchmark.Console
dotnet run -c Release
```

## 測試比較

測試包含三個場景：

1. **StandardHttpClient (基準)** - 標準的 HttpClient，沒有任何恢復機制
2. **ResilienceHttpClient** - 使用 Microsoft.Extensions.Http.Resilience 的 HttpClient
3. **PollyHttpClient** - 使用 Microsoft.Extensions.Http.Polly 的 HttpClient

## 套件版本

- **Microsoft.Extensions.Http.Resilience**: 9.9.0
- **Microsoft.Extensions.Http.Polly**: 9.0.9
- **BenchmarkDotNet**: 0.15.4

## 📊 測試結果

效能測試已完成！查看詳細報告：

### 📋 報告檔案
- **[完整效能報告](./PERFORMANCE_REPORT.md)** - 詳細的測試結果分析
- **[視覺化圖表](./PERFORMANCE_CHARTS.md)** - Mermaid 圖表展示
- **[測試差異分析](./BENCHMARK_ANALYSIS.md)** - 重要！解釋與網路文章結果差異的原因

### 🎯 關鍵發現
| 方法 | 平均時間 | 記憶體使用 | 效能開銷 |
|------|----------|------------|----------|
| **StandardHttpClient** (基準) | 375.2 μs | 3.31 KB | - |
| **PollyHttpClient** | 395.0 μs | 4.46 KB | +5.3% |
| **ResilienceHttpClient** | 407.0 μs | 6.48 KB | +8.5% |

### ⚠️ 重要發現
> **測試結果與網路文章相反的原因**：Microsoft.Extensions.Http.Polly (9.0.9) 實際使用的是 **Polly 7.2.4**（2021年舊版），而不是 Polly V8！詳見 [測試差異分析](./BENCHMARK_ANALYSIS.md)。

### 🔄 修正版測試
執行修正版測試來比較真正的 Polly V7 vs V8 vs Resilience：
```bash
cd src/HttpResilienceBenchmark.Console
echo "2" | dotnet run -c Release  # 選擇修正版測試
```

### 💡 目前建議 (基於實際可用套件)
- **高效能需求**: 使用 **Microsoft.Extensions.Http.Polly** (最佳平衡)
- **新專案**: 可考慮 **Microsoft.Extensions.Http.Resilience** (未來技術，但記憶體開銷較高)
- **極致效能**: 使用 **StandardHttpClient** (需自建重試邏輯)