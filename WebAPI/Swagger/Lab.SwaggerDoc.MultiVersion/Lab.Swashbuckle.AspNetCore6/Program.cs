using System.Reflection;
using System.Xml.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Employee API",
        Description = "An ASP.NET Core Web API for managing employees",
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
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",
        Title = "Employee API"
    });
    options.ExampleFilters();

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());
builder.Services.AddApiVersioning(option =>
{
    //返回響應標頭中支援的版本資訊
    option.ReportApiVersions = true;

    //未提供版本請請時，使用預設版號
    option.AssumeDefaultVersionWhenUnspecified = true;

    //預設api版本號，支援時間或數字版本號 
    option.DefaultApiVersion = new ApiVersion(1, 0);

    //支援MediaType、Header、QueryString 設定版本號；預設為 QueryString、UrlSegment
    option.ApiVersionReader = ApiVersionReader.Combine(
        new MediaTypeApiVersionReader("api-version"),
        new HeaderApiVersionReader("api-version"),
        new QueryStringApiVersionReader("api-version"),
        new UrlSegmentApiVersionReader());
});

// builder.Services.AddVersionedApiExplorer(options =>
// {
//     // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
//     // note: the specified format code will format the version as "'v'major[.minor][-status]"
//     options.GroupNameFormat = "'v'VVV";
//
//     // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
//     // can also be used to control the format of the API version in route templates
//     options.SubstituteApiVersionInUrl = true;
// });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseApiVersioning();

app.Run();