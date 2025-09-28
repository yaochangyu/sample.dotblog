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

Microsoft.AspNetCore.OpenApi 套件提供了內建的 OpenAPI 文件生成功能。如果需要使用 Swagger UI，可以選擇安裝 Swashbuckle.AspNetCore 套件，或使用自訂的 UI 實作。

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

## 建置時生成

許多 .NET 開發人員會發現在建置時生成 OpenAPI 文件的選項很有吸引力。要啟用建置時生成，只需新增 `Microsoft.Extensions.ApiDescription.Server` 套件到您的專案。

預設情況下，OpenAPI 文件生成到專案的 `obj` 目錄中，但您可以使用 `OpenApiDocumentsDirectory` 屬性自訂生成文件的位置：

```xml
<PropertyGroup>
  <OpenApiDocumentsDirectory>./</OpenApiDocumentsDirectory>
</PropertyGroup>
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

根據 [.NET 9 OpenAPI 博客文章](https://www.cnblogs.com/vipwan/p/18210947) 的建議，可以整合多種 UI 工具來展示 OpenAPI 文件。

### 1. SwaggerUI 整合

#### 方法一：使用 Swashbuckle.AspNetCore（需額外安裝套件）

如果您選擇使用 Swashbuckle.AspNetCore，需要先安裝套件：

```bash
dotnet add package Swashbuckle.AspNetCore
```

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.Run();
```

#### 方法二：自訂 SwaggerUI 擴展方法

```csharp
public static class SwaggerUiExtensions
{
    public static IEndpointConventionBuilder MapSwaggerUI(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/swagger", () =>
            Results.Content(GenerateSwaggerHtml(), "text/html"));
    }

    private static string GenerateSwaggerHtml()
    {
        return """
        <!DOCTYPE html>
        <html>
        <head>
            <title>Swagger UI</title>
            <link rel="stylesheet" type="text/css" href="https://unpkg.com/swagger-ui-dist@3.52.5/swagger-ui.css" />
        </head>
        <body>
            <div id="swagger-ui"></div>
            <script src="https://unpkg.com/swagger-ui-dist@3.52.5/swagger-ui-bundle.js"></script>
            <script>
                SwaggerUIBundle({
                    url: '/openapi/v1.json',
                    dom_id: '#swagger-ui',
                    presets: [
                        SwaggerUIBundle.presets.apis,
                        SwaggerUIBundle.presets.standalone
                    ]
                });
            </script>
        </body>
        </html>
        """;
    }
}
```

### 2. ScalarUI 整合

ScalarUI 是一個現代化的 OpenAPI 文件檢視器，提供更好的使用者體驗：

```csharp
public static class ScalarUiExtensions
{
    public static IEndpointConventionBuilder MapScalarUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/scalar/{documentName}", (string documentName) =>
            Results.Content(GenerateScalarHtml(documentName), "text/html"));
    }

    private static string GenerateScalarHtml(string documentName)
    {
        return $"""
        <!doctype html>
        <html>
        <head>
            <title>API Documentation</title>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
        </head>
        <body>
            <script
                id="api-reference"
                data-url="/openapi/{documentName}.json"
                data-configuration='{{"theme":"purple"}}'
            ></script>
            <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
        </body>
        </html>
        """;
    }
}
```

### 3. Redoc 整合

Redoc 提供了另一種優雅的文件展示方式：

```csharp
public static class RedocExtensions
{
    public static IEndpointConventionBuilder MapRedocUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/redoc/{documentName}", (string documentName) =>
            Results.Content(GenerateRedocHtml(documentName), "text/html"));
    }

    private static string GenerateRedocHtml(string documentName)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <title>API Documentation</title>
            <meta charset="utf-8"/>
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <link href="https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700" rel="stylesheet">
            <style>
                body {{ margin: 0; padding: 0; }}
            </style>
        </head>
        <body>
            <redoc spec-url='/openapi/{documentName}.json'></redoc>
            <script src="https://cdn.jsdelivr.net/npm/redoc@2.0.0/bundles/redoc.standalone.js"></script>
        </body>
        </html>
        """;
    }
}
```

### 4. 完整的 Program.cs 設定範例

將所有 UI 工具整合到應用程式中：

```csharp
var builder = WebApplication.CreateBuilder(args);

// 註冊 OpenAPI 服務
builder.Services.AddOpenApi();

var app = builder.Build();

// 設定 HTTP 請求管道
if (app.Environment.IsDevelopment())
{
    // 對應 OpenAPI 文件端點
    app.MapOpenApi();

    // 設定多種 UI 工具
    app.MapSwaggerUI();       // 可在 /swagger 存取
    app.MapScalarUi();        // 可在 /scalar/v1 存取
    app.MapRedocUi();         // 可在 /redoc/v1 存取

    // 或者使用統一的導覽頁面
    app.MapGet("/", () => Results.Content("""
        <html>
        <body>
            <h1>API 文件</h1>
            <ul>
                <li><a href="/swagger">Swagger UI</a></li>
                <li><a href="/scalar/v1">Scalar UI</a></li>
                <li><a href="/redoc/v1">Redoc UI</a></li>
                <li><a href="/openapi/v1.json">OpenAPI JSON</a></li>
            </ul>
        </body>
        </html>
        """, "text/html"));
}

// 定義 API 端點
app.MapGet("/weatherforecast", () =>
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

### 5. UI 工具比較

| 工具 | 特色 | 優點 | 缺點 |
|------|------|------|------|
| **Swagger UI** | 業界標準 | 成熟穩定、功能完整、測試功能 | 介面較傳統 |
| **Scalar UI** | 現代化設計 | 美觀、快速、互動性好 | 相對較新 |
| **Redoc** | 文件導向 | 適合文件閱讀、排版美觀 | 缺少測試功能 |

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

1. **建立擴展方法檔案**：建立 `Extensions/OpenApiUiExtensions.cs`
2. **實作各種 UI 擴展方法**：包含 SwaggerUI、ScalarUI、Redoc
3. **更新 Program.cs**：註冊服務並對應端點
4. **測試各種 UI**：確保所有介面正常運作
5. **自訂設定**：根據需要調整主題和配置

## 完整範例

```csharp
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 設定 OpenAPI 服務
builder.Services.AddOpenApi("v1", options =>
{
    options.OpenApiDocument.Info = new OpenApiInfo
    {
        Title = "我的 API",
        Version = "v1.0",
        Description = "使用 .NET 9 和 OpenAPI 的範例 API"
    };
});

// 如果使用 Swashbuckle.AspNetCore，需要新增以下服務
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// 設定 HTTP 請求管道
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "我的 API v1");
        c.RoutePrefix = string.Empty; // 在根路徑提供 Swagger UI
    });
}

// 定義端點
app.MapGet("/", () => "歡迎使用我的 API!")
   .WithName("GetWelcome")
   .WithOpenApi();

app.MapGet("/health", () => Results.Ok(new { Status = "健康" }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.Run();
```

## 參考資源

- [Microsoft Learn - ASP.NET Core 中的 OpenAPI 支援概述](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-9.0)
- [Microsoft Learn - 生成 OpenAPI 文件](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0)
- [Microsoft Learn - 建立 Minimal API 教學](https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-9.0)
- [.NET Blog - .NET 9 中的 OpenAPI 文件生成](https://devblogs.microsoft.com/dotnet/dotnet9-openapi/)
- [GitHub - ASP.NET Core 文件](https://github.com/dotnet/AspNetCore.Docs)
- [NuGet 套件頁面](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi)

Microsoft.AspNetCore.OpenApi 套件為 ASP.NET Core 應用程式中生成和提供 OpenAPI 文件提供了全面的解決方案，內建支援現代 OpenAPI 標準和廣泛的自訂選項。此設定提供了一個完整的 OpenAPI 文件生成解決方案，適用於 .NET 9 中的現代 Web API 開發。