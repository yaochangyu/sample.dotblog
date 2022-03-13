using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lab.Swashbuckle.AspNetCore6;

public class ConfigureApiVersionSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureApiVersionSwaggerGenOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        //產生 API 資訊
        var info = new OpenApiInfo
        {
            Version = description.ApiVersion.ToString(),
            Title = "Employee API",
            Description =
                @"<p>Sample API with versioning including Swagger.</p><p>Partly taken from <a href=""https://github.com/microsoft/aspnet-api-versioning"">this repository</a>.</p>",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new OpenApiContact
            {
                Name = "Example Contact",
                Url = new Uri("https://example.com/contact")
            },
            License = new OpenApiLicense
            {
                Name = "Example License",
                Url = new Uri("https://example.com/license")
            }
        };

        if (description.IsDeprecated)
        {
            info.Description +=
                @"<p><strong><span style=""color:white;background-color:red"">VERSION IS DEPRECATED</span></strong></p>";
        }

        return info;
    }
}