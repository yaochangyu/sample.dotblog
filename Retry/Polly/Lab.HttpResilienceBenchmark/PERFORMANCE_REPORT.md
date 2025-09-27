# HTTP 彈性機制效能比較報告

## 📋 測試概述

本報告比較了三種 HTTP 客戶端配置的效能表現：
- **StandardHttpClient** - 標準 HttpClient (基準)
- **ResilienceHttpClient** - 使用 Microsoft.Extensions.Http.Resilience
- **PollyHttpClient** - 使用 Microsoft.Extensions.Http.Polly

## 🖥️ 測試環境

- **作業系統**: Linux Ubuntu 24.04.3 LTS (Noble Numbat)
- **處理器**: Intel Core i9-14900HX 2.42GHz, 1 CPU, 32 logical 和 16 physical cores
- **.NET 版本**: .NET 9.0.4
- **BenchmarkDotNet 版本**: v0.15.4

## 📊 測試結果

| 方法 | 平均時間 (μs) | 誤差範圍 | 標準差 | 相對比例 | 比例誤差 | 記憶體分配 | 分配比例 |
|------|---------------|----------|--------|----------|----------|------------|----------|
| **StandardHttpClient** (基準) | 375.2 | ±12.82 | 37.80 | 1.01 | 0.15 | 3.31 KB | 1.00 |
| **PollyHttpClient** | 395.0 | ±9.48 | 27.81 | 1.06 | 0.14 | 4.46 KB | 1.35 |
| **ResilienceHttpClient** | 407.0 | ±13.05 | 38.47 | 1.10 | 0.16 | 6.48 KB | 1.96 |

## 📈 效能分析

### ⏱️ 執行時間分析
- **StandardHttpClient** (基準): 375.2 μs
- **PollyHttpClient**: 395.0 μs *(+5.3% 較慢)*
- **ResilienceHttpClient**: 407.0 μs *(+8.5% 較慢)*

### 🧠 記憶體使用分析
- **StandardHttpClient** (基準): 3.31 KB
- **PollyHttpClient**: 4.46 KB *(+35% 記憶體使用)*
- **ResilienceHttpClient**: 6.48 KB *(+96% 記憶體使用)*

## 🎯 關鍵發現

### ✅ 優勢
1. **Polly 表現優異**: 在效能和記憶體使用之間取得良好平衡
2. **效能差異可接受**: 所有彈性機制的額外開銷都在 10% 以內
3. **穩定性良好**: 所有方法都顯示相對穩定的效能表現

### ⚠️ 需要注意的點
1. **Resilience 記憶體開銷**: 新的 Microsoft.Extensions.Http.Resilience 套件使用了近乎兩倍的記憶體
2. **額外延遲**: 所有彈性機制都會增加 5-8% 的執行時間

## 📝 建議

### 🎯 選擇建議

1. **高效能需求場景** - 選擇 **PollyHttpClient**
   - 效能開銷最小 (+5.3%)
   - 記憶體使用合理 (+35%)
   - 成熟且穩定的解決方案

2. **新專案或長期考量** - 可考慮 **ResilienceHttpClient**
   - Microsoft 官方新一代解決方案
   - 更現代的 API 設計
   - 但需要接受較高的記憶體開銷 (+96%)

3. **極致效能需求** - 使用 **StandardHttpClient**
   - 最低的延遲和記憶體使用
   - 需要自己實作錯誤處理和重試邏輯

### 🔧 最佳化建議

1. **監控記憶體使用**: 特別是在使用 Microsoft.Extensions.Http.Resilience 時
2. **調整重試策略**: 根據實際需求調整重試次數和延遲
3. **定期效能測試**: 在不同負載下測試效能表現

## 🛠️ 技術細節

### 套件版本
- Microsoft.Extensions.Http.Resilience: 9.9.0
- Microsoft.Extensions.Http.Polly: 9.0.9
- BenchmarkDotNet: 0.15.4

### 測試配置
- 每個方法運行 100 次迭代
- 使用 Release 建置配置
- 包含記憶體診斷器 (MemoryDiagnoser)

## 📊 視覺化比較

```
效能比較 (執行時間):
StandardHttpClient  ████████████████████████████████████ 375.2 μs (基準)
PollyHttpClient     █████████████████████████████████████▌ 395.0 μs (+5.3%)
ResilienceHttpClient ██████████████████████████████████████▌ 407.0 μs (+8.5%)

記憶體使用比較:
StandardHttpClient  ████████████████████████████████████ 3.31 KB (基準)
PollyHttpClient     ████████████████████████████████████████████████ 4.46 KB (+35%)
ResilienceHttpClient ██████████████████████████████████████████████████████████████████ 6.48 KB (+96%)
```

## 🎉 結論

對於大多數應用程式，**Microsoft.Extensions.Http.Polly** 提供了效能和功能性之間的最佳平衡。雖然新的 Microsoft.Extensions.Http.Resilience 代表了未來的方向，但目前的記憶體開銷較高，需要根據具體使用場景進行評估。

測試日期: 2025-09-24