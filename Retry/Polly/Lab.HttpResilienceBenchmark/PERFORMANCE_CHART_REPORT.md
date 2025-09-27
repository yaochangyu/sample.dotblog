# HttpResilienceBenchmark 效能圖表報告

## 📊 效能測試視覺化分析

### 1. 平均執行時間比較

```mermaid
xychart-beta
    title "HTTP 客戶端平均執行時間比較 (微秒)"
    x-axis [StandardHttpClient, PollyV8HttpClient, ResilienceHttpClient, PollyV7HttpClient]
    y-axis "執行時間 (μs)" 0 --> 250
    bar [158.0, 171.9, 174.7, 204.3]
```

### 2. 相對效能比較 (以 StandardHttpClient 為基準)

```mermaid
xychart-beta
    title "相對效能比較 (基準 = 1.00)"
    x-axis [StandardHttpClient, PollyV8HttpClient, ResilienceHttpClient, PollyV7HttpClient]
    y-axis "相對比率" 0.9 --> 1.4
    line [1.00, 1.09, 1.11, 1.30]
```

### 3. 記憶體配置比較

```mermaid
xychart-beta
    title "記憶體配置比較 (KB)"
    x-axis [StandardHttpClient, PollyV8HttpClient, ResilienceHttpClient, PollyV7HttpClient]
    y-axis "記憶體使用 (KB)" 0 --> 7
    bar [3.31, 4.95, 6.48, 4.46]
```

### 4. 效能與記憶體使用關係圖

```mermaid
scatter-beta
    title "效能 vs 記憶體使用"
    x-axis "執行時間 (μs)" 150 --> 210
    y-axis "記憶體使用 (KB)" 3 --> 7

    point(158.0, 3.31) "StandardHttpClient"
    point(171.9, 4.95) "PollyV8HttpClient"
    point(174.7, 6.48) "ResilienceHttpClient"
    point(204.3, 4.46) "PollyV7HttpClient"
```

### 5. 效能等級分類

```mermaid
pie title 效能等級分布
    "優秀 (<160μs)" : 1
    "良好 (160-180μs)" : 2
    "普通 (>180μs)" : 1
```

### 6. 技術選型決策流程圖

```mermaid
flowchart TD
    A[需要 HTTP 彈性功能?] -->|否| B[使用 StandardHttpClient<br/>158μs, 3.31KB]
    A -->|是| C[重視效能還是整合性?]
    C -->|效能優先| D[使用 Polly V8<br/>171.9μs, 4.95KB]
    C -->|整合優先| E[使用 Microsoft.Extensions.Http.Resilience<br/>174.7μs, 6.48KB]
    C --> F[避免 Polly V7<br/>204.3μs, 4.46KB<br/>效能最差且不穩定]

    style B fill:#e1f5fe
    style D fill:#f3e5f5
    style E fill:#e8f5e8
    style F fill:#ffebee
```

### 7. 效能開銷分析

```mermaid
xychart-beta
    title "相對於基準的額外開銷百分比"
    x-axis [PollyV8HttpClient, ResilienceHttpClient, PollyV7HttpClient]
    y-axis "額外開銷 (%)" 0 --> 35
    bar [8.8, 10.6, 29.3]
```

### 8. 記憶體開銷分析

```mermaid
xychart-beta
    title "相對於基準的記憶體開銷百分比"
    x-axis [PollyV7HttpClient, PollyV8HttpClient, ResilienceHttpClient]
    y-axis "記憶體開銷 (%)" 0 --> 100
    bar [35, 49, 96]
```

## 📈 關鍵洞察

### 效能排名
1. **🥇 StandardHttpClient**: 158.0μs (基準)
2. **🥈 PollyV8HttpClient**: 171.9μs (+8.8%)
3. **🥉 ResilienceHttpClient**: 174.7μs (+10.6%)
4. **❌ PollyV7HttpClient**: 204.3μs (+29.3%)

### 記憶體使用排名
1. **🥇 StandardHttpClient**: 3.31KB (最少)
2. **🥈 PollyV7HttpClient**: 4.46KB (+35%)
3. **🥉 PollyV8HttpClient**: 4.95KB (+49%)
4. **❌ ResilienceHttpClient**: 6.48KB (+96%)

## 🎯 建議總結

```mermaid
mindmap
  root((HTTP 彈性方案選擇))
    生產環境推薦
      Polly V8
        效能優秀
        穩定性高
      MS Resilience
        官方支援
        整合性佳
    避免使用
      Polly V7
        效能最差
        變異性高
    成本考量
      效能成本
        8-30% 額外時間
      記憶體成本
        35-96% 額外空間
```

## 📊 測試數據摘要

| 指標 | StandardHttpClient | PollyV8HttpClient | ResilienceHttpClient | PollyV7HttpClient |
|------|-------------------|-------------------|---------------------|-------------------|
| **平均時間** | 158.0μs | 171.9μs | 174.7μs | 204.3μs |
| **標準差** | 6.68μs | 8.32μs | 8.46μs | 33.64μs |
| **記憶體使用** | 3.31KB | 4.95KB | 6.48KB | 4.46KB |
| **穩定性** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **推薦度** | 無彈性需求時 | 🔥 強烈推薦 | ✅ 推薦 | ❌ 不推薦 |