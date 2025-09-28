namespace Lab.AspNetCoreOpenApi.Extensions;

public static class OpenApiUiExtensions
{
    public static IEndpointConventionBuilder MapSwaggerUI(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/swagger", () =>
            Results.Content(GenerateSwaggerHtml(), "text/html; charset=utf-8"));
    }

    public static IEndpointConventionBuilder MapScalarUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/scalar/{documentName}", (string documentName) =>
            Results.Content(GenerateScalarHtml(documentName), "text/html; charset=utf-8"));
    }

    public static IEndpointConventionBuilder MapRedocUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/redoc/{documentName}", (string documentName) =>
            Results.Content(GenerateRedocHtml(documentName), "text/html; charset=utf-8"));
    }

    public static IEndpointConventionBuilder MapRapiDocUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/rapidoc/{documentName}", (string documentName) =>
            Results.Content(GenerateRapiDocHtml(documentName), "text/html; charset=utf-8"));
    }

    public static IEndpointConventionBuilder MapElementsUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/elements/{documentName}", (string documentName) =>
            Results.Content(GenerateElementsHtml(documentName), "text/html; charset=utf-8"));
    }

    public static IEndpointConventionBuilder MapOpenApiExplorerUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/explorer/{documentName}", (string documentName) =>
            Results.Content(GenerateOpenApiExplorerHtml(documentName), "text/html; charset=utf-8"));
    }

    public static IEndpointConventionBuilder MapApiDocsNavigator(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", () =>
            Results.Content(GenerateNavigatorHtml(), "text/html; charset=utf-8"));
    }

    private static string GenerateSwaggerHtml()
    {
        return """
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
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

    private static string GenerateScalarHtml(string documentName)
    {
        return $$"""
        <!doctype html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API Documentation - Scalar UI</title>
        </head>
        <body>
            <script
                id="api-reference"
                data-url="/openapi/{{documentName}}.json"
                data-configuration='{"theme":"purple"}'
            ></script>
            <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
        </body>
        </html>
        """;
    }

    private static string GenerateRedocHtml(string documentName)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API Documentation - Redoc</title>
            <link href="https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700" rel="stylesheet">
            <style>
                body { margin: 0; padding: 0; }
            </style>
        </head>
        <body>
            <redoc spec-url='/openapi/{{documentName}}.json'></redoc>
            <script src="https://cdn.jsdelivr.net/npm/redoc@2.0.0/bundles/redoc.standalone.js"></script>
        </body>
        </html>
        """;
    }

    private static string GenerateRapiDocHtml(string documentName)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API Documentation - RapiDoc</title>
            <style>
                body { margin: 0; padding: 0; }
                rapi-doc {
                    width: 100%;
                    height: 100vh;
                }
            </style>
        </head>
        <body>
            <rapi-doc
                spec-url="/openapi/{{documentName}}.json"
                theme="dark"
                render-style="read"
                show-header="false"
                allow-try="true"
                allow-server-selection="true"
                allow-authentication="true">
            </rapi-doc>
            <script type="module" src="https://unpkg.com/rapidoc/dist/rapidoc-min.js"></script>
        </body>
        </html>
        """;
    }

    private static string GenerateElementsHtml(string documentName)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API Documentation - Elements</title>
            <link rel="stylesheet" href="https://unpkg.com/@stoplight/elements/styles.min.css" />
            <style>
                body { margin: 0; padding: 0; }
            </style>
        </head>
        <body>
            <elements-api
                apiDescriptionUrl="/openapi/{{documentName}}.json"
                router="hash"
                layout="sidebar">
            </elements-api>
            <script src="https://unpkg.com/@stoplight/elements/web-components.min.js"></script>
        </body>
        </html>
        """;
    }

    private static string GenerateOpenApiExplorerHtml(string documentName)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API Documentation - OpenAPI Explorer</title>
            <style>
                body {
                    margin: 0;
                    padding: 20px;
                    font-family: 'Segoe UI', Arial, sans-serif;
                    background: #f5f5f5;
                }
                .container {
                    max-width: 1200px;
                    margin: 0 auto;
                    background: white;
                    border-radius: 8px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    overflow: hidden;
                }
                .header {
                    background: #1976d2;
                    color: white;
                    padding: 20px;
                    text-align: center;
                }
                .content {
                    padding: 20px;
                }
                .explorer {
                    width: 100%;
                    height: 70vh;
                    border: 1px solid #ddd;
                    border-radius: 4px;
                }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <h1>OpenAPI Explorer</h1>
                    <p>輕量級 API 文件瀏覽器</p>
                </div>
                <div class="content">
                    <iframe
                        class="explorer"
                        src="https://editor.swagger.io/?url=/openapi/{{documentName}}.json"
                        title="OpenAPI Explorer">
                    </iframe>
                </div>
            </div>
        </body>
        </html>
        """;
    }

    private static string GenerateNavigatorHtml()
    {
        return """
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>API 文件導覽</title>
            <style>
                * {
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }
                body {
                    font-family: 'Segoe UI', 'Microsoft JhengHei', Arial, sans-serif;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    min-height: 100vh;
                    padding: 20px;
                }
                .container {
                    max-width: 1200px;
                    margin: 0 auto;
                    background: white;
                    border-radius: 16px;
                    box-shadow: 0 20px 40px rgba(0,0,0,0.1);
                    overflow: hidden;
                }
                .header {
                    background: linear-gradient(135deg, #1976d2 0%, #1565c0 100%);
                    color: white;
                    padding: 40px;
                    text-align: center;
                }
                .header h1 {
                    font-size: 2.5rem;
                    margin-bottom: 10px;
                    font-weight: 300;
                }
                .header p {
                    font-size: 1.1rem;
                    opacity: 0.9;
                }
                .grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
                    gap: 20px;
                    padding: 40px;
                }
                .card {
                    background: white;
                    border: 1px solid #e0e0e0;
                    border-radius: 12px;
                    padding: 24px;
                    transition: all 0.3s ease;
                    text-decoration: none;
                    color: inherit;
                    position: relative;
                    overflow: hidden;
                }
                .card::before {
                    content: '';
                    position: absolute;
                    top: 0;
                    left: 0;
                    right: 0;
                    height: 4px;
                    background: var(--accent-color);
                    transform: scaleX(0);
                    transition: transform 0.3s ease;
                }
                .card:hover::before {
                    transform: scaleX(1);
                }
                .card:hover {
                    transform: translateY(-4px);
                    box-shadow: 0 12px 24px rgba(0,0,0,0.15);
                }
                .card-icon {
                    width: 48px;
                    height: 48px;
                    border-radius: 12px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 24px;
                    margin-bottom: 16px;
                    background: var(--accent-color);
                    color: white;
                }
                .card h3 {
                    font-size: 1.3rem;
                    margin-bottom: 8px;
                    color: #333;
                }
                .card p {
                    color: #666;
                    line-height: 1.5;
                    margin-bottom: 16px;
                }
                .card-features {
                    list-style: none;
                }
                .card-features li {
                    color: #888;
                    font-size: 0.9rem;
                    margin-bottom: 4px;
                    position: relative;
                    padding-left: 16px;
                }
                .card-features li::before {
                    content: '✓';
                    position: absolute;
                    left: 0;
                    color: var(--accent-color);
                    font-weight: bold;
                }

                /* 個別卡片色彩 */
                .swagger { --accent-color: #85ea2d; }
                .scalar { --accent-color: #9333ea; }
                .redoc { --accent-color: #f97316; }
                .rapidoc { --accent-color: #06b6d4; }
                .elements { --accent-color: #10b981; }
                .explorer { --accent-color: #3b82f6; }
                .json { --accent-color: #6b7280; }

                .footer {
                    text-align: center;
                    padding: 20px;
                    color: #666;
                    background: #f8f9fa;
                    border-top: 1px solid #e0e0e0;
                }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <h1>🚀 API 文件中心</h1>
                    <p>選擇您偏好的 API 文件檢視器</p>
                </div>

                <div class="grid">
                    <a href="/swagger" class="card swagger">
                        <div class="card-icon">🎯</div>
                        <h3>Swagger UI</h3>
                        <p>業界標準的 API 文件工具，功能完整且穩定。</p>
                        <ul class="card-features">
                            <li>互動式 API 測試</li>
                            <li>完整的 OpenAPI 支援</li>
                            <li>成熟穩定</li>
                        </ul>
                    </a>

                    <a href="/scalar/v1" class="card scalar">
                        <div class="card-icon">✨</div>
                        <h3>Scalar UI</h3>
                        <p>現代化設計的 API 文件檢視器，提供優美的使用者體驗。</p>
                        <ul class="card-features">
                            <li>現代化介面設計</li>
                            <li>快速載入</li>
                            <li>良好的互動性</li>
                        </ul>
                    </a>

                    <a href="/redoc/v1" class="card redoc">
                        <div class="card-icon">📖</div>
                        <h3>Redoc</h3>
                        <p>專注於文件閱讀體驗的檢視器，排版美觀。</p>
                        <ul class="card-features">
                            <li>優美的文件排版</li>
                            <li>響應式設計</li>
                            <li>適合文件閱讀</li>
                        </ul>
                    </a>

                    <a href="/rapidoc/v1" class="card rapidoc">
                        <div class="card-icon">⚡</div>
                        <h3>RapiDoc</h3>
                        <p>輕量級且高度可客製化的 Web Component 檢視器。</p>
                        <ul class="card-features">
                            <li>輕量快速</li>
                            <li>高度可客製化</li>
                            <li>Web Component 架構</li>
                        </ul>
                    </a>

                    <a href="/elements/v1" class="card elements">
                        <div class="card-icon">🔧</div>
                        <h3>Elements</h3>
                        <p>由 Stoplight 開發，專注於開發者體驗的檢視器。</p>
                        <ul class="card-features">
                            <li>開發者友善</li>
                            <li>企業級功能</li>
                            <li>Stoplight 生態系</li>
                        </ul>
                    </a>

                    <a href="/explorer/v1" class="card explorer">
                        <div class="card-icon">🔍</div>
                        <h3>OpenAPI Explorer</h3>
                        <p>基於 Swagger Editor 的輕量級 API 探索工具。</p>
                        <ul class="card-features">
                            <li>簡潔介面</li>
                            <li>專注於探索</li>
                            <li>Swagger Editor 支援</li>
                        </ul>
                    </a>

                    <a href="/openapi/v1.json" class="card json">
                        <div class="card-icon">📄</div>
                        <h3>原始 JSON</h3>
                        <p>檢視原始的 OpenAPI 規範 JSON 文件。</p>
                        <ul class="card-features">
                            <li>原始資料格式</li>
                            <li>程式化存取</li>
                            <li>標準 OpenAPI 3.1</li>
                        </ul>
                    </a>
                </div>

                <div class="footer">
                    <p>🔧 基於 Microsoft.AspNetCore.OpenApi (.NET 9) 建構</p>
                </div>
            </div>
        </body>
        </html>
        """;
    }
}