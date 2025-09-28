using Lab.AspNetCoreOpenApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 對應 OpenAPI 文件端點
    app.MapOpenApi();

    // 設定多種 UI 工具
    app.MapSwaggerUI();
    app.MapScalarUi();
    app.MapRedocUi();
    
    // 統一的導覽頁面
    app.MapGet("/", () => Results.Content(
        """
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API 文件導覽</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 40px; }
                ul { list-style-type: none; padding: 0; }
                li { margin: 10px 0; }
                a {
                    display: inline-block;
                    padding: 10px 15px;
                    background-color: #007acc;
                    color: white;
                    text-decoration: none;
                    border-radius: 5px;
                    min-width: 120px;
                    text-align: center;
                }
                a:hover { background-color: #005a9e; }
            </style>
        </head>
        <body>
            <h1>🚀 API 文件導覽</h1>
            <p>選擇您偏好的文件檢視工具：</p>
            <ul>
                <li><a href="/swagger">📋 Swagger UI</a> - 業界標準，具備測試功能</li>
                <li><a href="/scalar/v1">✨ Scalar UI</a> - 現代化設計，美觀快速</li>
                <li><a href="/redoc/v1">📖 Redoc UI</a> - 文件導向，排版美觀</li>
                <li><a href="/openapi/v1.json">🔗 OpenAPI JSON</a> - 原始規格檔</li>
            </ul>
        </body>
        </html>
        """,
        "text/html; charset=utf-8"));
}

app.UseHttpsRedirection();

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
    .WithName("GetWeatherForecast")
    ;

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}