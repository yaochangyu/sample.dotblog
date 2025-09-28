# Microsoft.AspNetCore.OpenApi 完整使用指南 (.NET 9)

## 概述

Microsoft.AspNetCore.OpenApi 為 .NET 9 中的 ASP.NET Core 應用程式提供內建的 OpenAPI 文件生成支援。該套件支援 Minimal APIs 和控制器型應用程式，提供更整合和無縫的開發體驗。

### 主要功能

- **支援生成 OpenAPI 3.1 版本文件**
- **支援 JSON Schema draft 2020-12**
- **運行時文件生成**：支援在運行時生成 OpenAPI 文件並透過端點存取
- **建置時文件生成**：支援在建置時生成 OpenAPI 文件
- **文件轉換器**：支援透過文件轉換器 API 自訂生成的文件
- **多文件支援**：支援從單一應用程式生成多個 OpenAPI 文件
- **參數化端點**：透過參數化端點檢視生成的 OpenAPI 文件 (`/openapi/{documentName}.json`)
- **利用 System.Text.Json 提供的 JSON schema 支援**
- **與原生 AOT 相容**

## 套件安裝

### 使用 .NET CLI

```bash
dotnet add package Microsoft.AspNetCore.OpenApi
```

### 專案檔案配置

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <PropertyGroup>
    <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
    <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)</OpenApiDocumentsDirectory>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.*" />
    <PackageReference Include="Microsoft.Extensions.ApiDescription.Server" Version="9.0.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

Microsoft.AspNetCore.OpenApi 套件提供了內建的 OpenAPI 文件生成功能。不需要額外的套件，即可生成 OpenAPI 文件，這時候可以選擇性地整合其他 UI 工具來檢視這些文件。

## 基本實作範例

### Program.cs 基本設定

AddOpenApi 將 OpenAPI 文件生成所需的服務註冊到應用程式的 DI 容器中。MapOpenApi 在應用程式中新增一個端點，用於檢視序列化為 JSON 的 OpenAPI 文件。

```csharp
var builder = WebApplication.CreateBuilder();

// 註冊 OpenAPI 所需的服務
builder.Services.AddOpenApi();

var app = builder.Build();

// 新增 /openapi/{documentName}.json 端點到應用程式
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
```

## OpenAPI 端點配置

預設情況下，透過呼叫 MapOpenApi 註冊的 OpenAPI 端點在 `/openapi/{documentName}.json` 端點公開文件。

以下程式碼示範如何自訂註冊 OpenAPI 文件的路由：

```csharp
app.MapOpenApi("/openapi/{documentName}/openapi.json");
```

## 文件名稱

應用程式中每個 OpenAPI 文件都有一個唯一的名稱。註冊的預設文件名稱是 `v1`。

文件名稱可以透過將名稱作為參數傳遞給 AddOpenApi 呼叫來修改：

```csharp
builder.Services.AddOpenApi("internal"); // 文件名稱是 internal
```

## UI 工具整合方案

根據本專案實際實作，已整合 6 種不同的 OpenAPI 文件檢視工具，提供完整的視覺化介面選擇。

### 1. 專案架構

本專案使用 **Extensions/OpenApiUiExtensions.cs** 實作所有 UI 擴展方法：

```
Lab.AspNetCoreOpenApi/
├── Extensions/
│   └── OpenApiUiExtensions.cs    # 包含所有 UI 檢視器的擴展方法
├── Program.cs                    # 主程式，設定路由和服務
├── Lab.AspNetCoreOpenApi.csproj  # 專案檔案
└── Properties/launchSettings.json # 啟動設定
```

### 2. 已實作的 UI 檢視工具

#### 2.1 Swagger UI
- **路由**：`/swagger`
- **特色**：業界標準，互動式 API 測試
- **技術**：使用 unpkg.com CDN 載入 swagger-ui-dist

```csharp
public static IEndpointConventionBuilder MapSwaggerUI(this IEndpointRouteBuilder endpoints)
{
    return endpoints.MapGet("/swagger", () =>
        Results.Content(GenerateSwaggerHtml(), "text/html; charset=utf-8"));
}
```

#### 2.2 Scalar UI
- **路由**：`/scalar/{documentName}`
- **特色**：現代化設計，紫色主題
- **技術**：使用 @scalar/api-reference CDN

#### 2.3 Redoc
- **路由**：`/redoc/{documentName}`
- **特色**：文件導向，優美排版
- **技術**：使用 redoc standalone bundle

#### 2.4 RapiDoc
- **路由**：`/rapidoc/{documentName}`
- **特色**：Web Component 架構，深色主題
- **技術**：使用 rapidoc-min.js 模組

#### 2.5 Elements
- **路由**：`/elements/{documentName}`
- **特色**：Stoplight 生態系，企業級功能
- **技術**：使用 @stoplight/elements Web Components

#### 2.6 OpenAPI Explorer
- **路由**：`/explorer/{documentName}`
- **特色**：基於 Swagger Editor 的輕量級探索工具
- **技術**：使用 iframe 嵌入 editor.swagger.io

### 3. 美觀的導覽頁面

#### 3.1 統一導覽介面
- **路由**：`/`（首頁）
- **功能**：提供所有檢視工具的統一入口
- **設計**：現代化卡片佈局，漸層背景，互動式動效

#### 3.2 導覽頁面特色
- 響應式網格佈局（Grid Layout）
- 每個工具使用獨特的品牌色彩
- Hover 效果和動畫轉場
- 卡片式設計，包含圖示、描述和功能特色

### 4. 完整的 Program.cs 實作

```csharp
using Lab.AspNetCoreOpenApi.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // 對應 OpenAPI 文件端點
    app.MapOpenApi();

    // 設定多種 UI 工具
    app.MapSwaggerUI();           // /swagger
    app.MapScalarUi();            // /scalar/v1
    app.MapRedocUi();             // /redoc/v1
    app.MapRapiDocUi();           // /rapidoc/v1
    app.MapElementsUi();          // /elements/v1
    app.MapOpenApiExplorerUi();   // /explorer/v1

    // 統一的導覽頁面（美觀版本）
    app.MapApiDocsNavigator();    // /
}

app.UseHttpsRedirection();

// WeatherForecast API 端點
app.MapGet("/weatherforecast", () => { /* ... */ })
    .WithName("GetWeatherForecast");

app.Run();
```

### 5. 專案檔案配置

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.8"/>
    </ItemGroup>
</Project>
```

### 6. 啟動設定

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5036"
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7149;http://localhost:5036"
    }
  }
}
```

### 7. UI 工具完整比較

| 工具 | 路由 | 特色 | 優點 | 適用場景 |
|------|------|------|------|----------|
| **Swagger UI** | `/swagger` | 業界標準 | 成熟穩定、功能完整、測試功能 | 日常開發測試 |
| **Scalar UI** | `/scalar/v1` | 現代化設計 | 美觀、快速、互動性好 | 現代化專案展示 |
| **Redoc** | `/redoc/v1` | 文件導向 | 適合文件閱讀、排版美觀 | 文件閱讀和展示 |
| **RapiDoc** | `/rapidoc/v1` | Web Component | 輕量快速、高度可客製化 | 輕量級整合 |
| **Elements** | `/elements/v1` | 企業級 | 開發者友善、企業級功能 | 企業級應用 |
| **OpenAPI Explorer** | `/explorer/v1` | 探索工具 | 基於 Swagger Editor、簡潔介面 | API 探索和學習 |

### 8. 存取方式

啟動應用程式後，可透過以下網址存取：

- **導覽頁面**：http://localhost:5036/
- **各種檢視器**：
  - Swagger UI: http://localhost:5036/swagger
  - Scalar UI: http://localhost:5036/scalar/v1
  - Redoc: http://localhost:5036/redoc/v1
  - RapiDoc: http://localhost:5036/rapidoc/v1
  - Elements: http://localhost:5036/elements/v1
  - OpenAPI Explorer: http://localhost:5036/explorer/v1
- **原始 JSON**：http://localhost:5036/openapi/v1.json

## Minimal API 範例

### 基本端點設定

```csharp
app.MapGet("/", () => "Hello World!")
   .WithName("GetHello")
   .WithOpenApi();

app.MapGet("/weather", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

### 進階端點配置

```csharp
app.MapPost("/users", (CreateUserRequest request) =>
{
    // 建立使用者邏輯
    var user = new User(Guid.NewGuid(), request.Name, request.Email);
    return Results.Created($"/users/{user.Id}", user);
})
.WithName("CreateUser")
.WithOpenApi(operation => new(operation)
{
    Summary = "建立新使用者",
    Description = "建立一個新的使用者帳號",
    Tags = new List<OpenApiTag> { new() { Name = "Users" } }
})
.Produces<User>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.Accepts<CreateUserRequest>("application/json");

record CreateUserRequest(string Name, string Email);
record User(Guid Id, string Name, string Email);
```

## 配置和自訂

### 文件群組

使用 `WithGroupName` 或 `EndpointGroupName` 屬性決定要包含在文件中的端點：

```csharp
app.MapGet("/admin/users", () => "Admin users")
   .WithGroupName("admin")
   .WithOpenApi();

app.MapGet("/public/info", () => "Public info")
   .WithGroupName("public")
   .WithOpenApi();
```

### 回應元資料

在 Minimal API 應用程式中，ASP.NET Core 可以提取由端點上的擴充方法、路由處理器上的屬性和路由處理器的回傳型別新增的回應元資料：

```csharp
app.MapGet("/products/{id}", (int id) =>
{
    // 產品查詢邏輯
    return TypedResults.Ok(new Product(id, "產品名稱"));
})
.WithName("GetProduct")
.WithOpenApi()
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

record Product(int Id, string Name);
```

### 自訂 OpenAPI 文件

```csharp
builder.Services.ConfigureOpenApi(options =>
{
    options.UseTransformer<BearerSecuritySchemeTransformer>();
});

public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var requirements = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token"
            }
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = requirements;

        return Task.CompletedTask;
    }
}
```

## 多個 OpenAPI 文件

```csharp
builder.Services.AddOpenApi("public");
builder.Services.AddOpenApi("internal");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/public.json").WithGroupName("public");
    app.MapOpenApi("/openapi/internal.json").WithGroupName("internal");
}
```

## 程式化存取

您可以透過相依性注入將 IOpenApiDocumentProvider 注入到您的服務中，以程式化方式存取 OpenAPI 文件，即使在 HTTP 請求上下文之外也可以。這使得生成客戶端 SDK 或驗證 API 合約等場景成為可能。

```csharp
app.MapGet("/openapi-info", (IOpenApiDocumentProvider documentProvider) =>
{
    var document = documentProvider.GetOpenApiDocument("v1");
    return Results.Ok(new { Title = document.Info.Title, Version = document.Info.Version });
});
```

## 快取配置

在適用的情況下，可以快取 OpenAPI 文件以避免在每個 HTTP 請求上執行文件生成管道。您可以使用輸出快取來提高效能。

```csharp
builder.Services.AddOutputCache();

var app = builder.Build();

app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().CacheOutput();
}
```

## 安全性考量

OpenAPI 端點僅限於開發環境，以降低暴露敏感資訊的風險並減少生產環境中的漏洞。

作為限制資訊揭露的安全性最佳實踐，OpenAPI 使用者介面（Swagger UI、ReDoc、Scalar）應僅在開發環境中啟用。

- 生產環境中應停用 OpenAPI 使用者介面（Swagger UI、ReDoc、Scalar）
- 避免在 OpenAPI 文件中暴露敏感的內部實作細節

## 最佳實踐

1. **使用 TypedResults**：回傳 TypedResults 而非 Results 有多個優點，包括可測試性和自動回傳回應型別元資料
2. **適當的錯誤處理**：為每個端點定義適當的錯誤回應
3. **文件分組**：使用群組來組織相關的端點
4. **版本控制**：為不同的 API 版本建立不同的 OpenAPI 文件
5. **安全性架構**：為需要驗證的端點定義安全性架構

## 實作步驟

本專案已完成所有實作步驟：

1. **✅ 建立擴展方法檔案**：已建立 `Extensions/OpenApiUiExtensions.cs`
2. **✅ 實作各種 UI 擴展方法**：已包含 6 種 UI 檢視器（Swagger UI、Scalar UI、Redoc、RapiDoc、Elements、OpenAPI Explorer）
3. **✅ 更新 Program.cs**：已註冊 OpenAPI 服務並對應所有端點
4. **✅ 實作美觀導覽頁面**：已建立統一的卡片式導覽介面
5. **✅ 專案配置完成**：已設定專案檔案和啟動設定

## 完整範例（實際專案實作）

本專案的實際 Program.cs 實作：

```csharp
using Lab.AspNetCoreOpenApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 註冊 OpenAPI 所需的服務
builder.Services.AddOpenApi();

var app = builder.Build();

// 設定 HTTP 請求管道
if (app.Environment.IsDevelopment())
{
    // 對應 OpenAPI 文件端點
    app.MapOpenApi();

    // 設定多種 UI 工具
    app.MapSwaggerUI();           // 可在 /swagger 存取
    app.MapScalarUi();            // 可在 /scalar/v1 存取
    app.MapRedocUi();             // 可在 /redoc/v1 存取
    app.MapRapiDocUi();           // 可在 /rapidoc/v1 存取
    app.MapElementsUi();          // 可在 /elements/v1 存取
    app.MapOpenApiExplorerUi();   // 可在 /explorer/v1 存取

    // 統一的導覽頁面（使用新的美觀版本）
    app.MapApiDocsNavigator();    // 可在 / 存取
}

app.UseHttpsRedirection();

// WeatherForecast API 端點
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

### 專案特色

1. **6 種 UI 檢視器整合**：提供多樣化的文件檢視選擇
2. **美觀的統一導覽頁面**：卡片式佈局，漸層背景，動畫效果
3. **簡潔的架構設計**：使用 Extensions 模式組織程式碼
4. **完整的中文支援**：所有介面都支援繁體中文
5. **無額外套件依賴**：僅使用 Microsoft.AspNetCore.OpenApi

## 參考資源

- [Microsoft Learn - ASP.NET Core 中的 OpenAPI 支援概述](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-9.0)
- [Microsoft Learn - 生成 OpenAPI 文件](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0)
- [Microsoft Learn - 建立 Minimal API 教學](https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-9.0)
- [.NET Blog - .NET 9 中的 OpenAPI 文件生成](https://devblogs.microsoft.com/dotnet/dotnet9-openapi/)
- [GitHub - ASP.NET Core 文件](https://github.com/dotnet/AspNetCore.Docs)
- [NuGet 套件頁面](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi)

Microsoft.AspNetCore.OpenApi 套件為 ASP.NET Core 應用程式中生成和提供 OpenAPI 文件提供了全面的解決方案，內建支援現代 OpenAPI 標準和廣泛的自訂選項。此設定提供了一個完整的 OpenAPI 文件生成解決方案，適用於 .NET 9 中的現代 Web API 開發。