# 效能比較視覺化圖表

## 🚀 測試流程

```mermaid
graph TD
    A[啟動 Web API 服務<br/>localhost:5068/api/members] --> B[初始化測試環境]
    B --> C[設定 HttpClient 實例]

    C --> D[StandardHttpClient<br/>標準 HttpClient]
    C --> E[PollyHttpClient<br/>使用 Polly 重試機制]
    C --> F[ResilienceHttpClient<br/>使用 Microsoft Resilience]

    D --> G[執行 100 次迭代測試]
    E --> G
    F --> G

    G --> H[收集效能數據]
    H --> I[產生測試報告]

    style A fill:#e1f5fe
    style G fill:#f3e5f5
    style I fill:#e8f5e8
```

## ⏱️ 執行時間比較

```mermaid
xychart-beta
    title "HTTP Client 執行時間比較 (微秒)"
    x-axis [StandardHttpClient, PollyHttpClient, ResilienceHttpClient]
    y-axis "執行時間 (μs)" 350 --> 420
    bar [375.2, 395.0, 407.0]
```

## 🧠 記憶體使用比較

```mermaid
xychart-beta
    title "HTTP Client 記憶體使用比較 (KB)"
    x-axis [StandardHttpClient, PollyHttpClient, ResilienceHttpClient]
    y-axis "記憶體使用 (KB)" 0 --> 7
    bar [3.31, 4.46, 6.48]
```

## 📊 效能差異百分比

```mermaid
pie title HTTP Client 效能開銷
    "StandardHttpClient (基準)" : 0
    "PollyHttpClient (+5.3%)" : 5.3
    "ResilienceHttpClient (+8.5%)" : 8.5
```

## 💾 記憶體開銷分析

```mermaid
pie title 記憶體使用分布
    "StandardHttpClient (3.31KB)" : 3.31
    "PollyHttpClient (4.46KB)" : 4.46
    "ResilienceHttpClient (6.48KB)" : 6.48
```

## 🎯 效能與功能平衡

```mermaid
quadrantChart
    title HTTP Client 效能 vs 功能性分析
    x-axis 低記憶體使用 --> 高記憶體使用
    y-axis 慢速度 --> 快速度

    quadrant-1 高效能/低記憶體
    quadrant-2 高效能/高記憶體
    quadrant-3 低效能/低記憶體
    quadrant-4 低效能/高記憶體

    StandardHttpClient: [0.1, 0.9]
    PollyHttpClient: [0.4, 0.7]
    ResilienceHttpClient: [0.8, 0.6]
```

## 🔄 測試架構流程

```mermaid
sequenceDiagram
    participant C as 測試客戶端
    participant S as StandardHttpClient
    participant P as PollyHttpClient
    participant R as ResilienceHttpClient
    participant API as Web API

    Note over C,API: 測試初始化階段
    C->>S: 建立標準客戶端
    C->>P: 建立 Polly 客戶端 (重試策略)
    C->>R: 建立 Resilience 客戶端 (標準彈性策略)

    Note over C,API: 效能測試階段 (100次迭代)
    loop 100 次迭代
        par 平行測試
            C->>+S: GET /api/members
            S->>+API: HTTP Request
            API-->>-S: {"name":"小章","age":18}
            S-->>-C: 375.2μs (平均)
        and
            C->>+P: GET /api/members
            P->>+API: HTTP Request (with retry)
            API-->>-P: {"name":"小章","age":18}
            P-->>-C: 395.0μs (平均)
        and
            C->>+R: GET /api/members
            R->>+API: HTTP Request (with resilience)
            API-->>-R: {"name":"小章","age":18}
            R-->>-C: 407.0μs (平均)
        end
    end

    Note over C,API: 數據收集與分析
    C->>C: 分析執行時間統計
    C->>C: 分析記憶體使用情況
    C->>C: 產生效能報告
```

## 📈 趨勢分析

```mermaid
gitgraph
    commit id: "StandardHttpClient"
    commit id: "基準測試: 375.2μs"

    branch polly-client
    commit id: "PollyHttpClient"
    commit id: "增加 5.3% 延遲"
    commit id: "增加 35% 記憶體"

    checkout main
    branch resilience-client
    commit id: "ResilienceHttpClient"
    commit id: "增加 8.5% 延遲"
    commit id: "增加 96% 記憶體"

    checkout main
    merge polly-client
    merge resilience-client
    commit id: "效能測試完成"
```

## 🎉 總結建議

```mermaid
flowchart TD
    A[選擇 HTTP 彈性策略] --> B{主要考量因素}

    B -->|極致效能| C[StandardHttpClient<br/>🏆 最快: 375.2μs<br/>💾 最省: 3.31KB]
    B -->|平衡考量| D[PollyHttpClient<br/>⚖️ 平衡: +5.3% 時間<br/>🛡️ 成熟: +35% 記憶體]
    B -->|未來導向| E[ResilienceHttpClient<br/>🚀 新技術: +8.5% 時間<br/>⚠️ 高開銷: +96% 記憶體]

    C --> F[適用場景:<br/>• 高頻率 API 呼叫<br/>• 記憶體敏感應用<br/>• 自建重試邏輯]
    D --> G[適用場景:<br/>• 一般 Web 應用<br/>• 需要重試機制<br/>• 生產環境穩定]
    E --> H[適用場景:<br/>• 新專案開發<br/>• 不敏感記憶體<br/>• 探索新功能]

    style C fill:#e8f5e8
    style D fill:#fff3e0
    style E fill:#fce4ec
```