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
}