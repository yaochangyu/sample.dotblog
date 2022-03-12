using System.Reflection;
using System.Xml.XPath;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

void IncludeXmlComments(Assembly assembly, SwaggerGenOptions swaggerGenOptions)
{
    var directory = AppDomain.CurrentDomain.BaseDirectory;
    if (assembly != null)
    {
        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(x => x.ToUpper()
                                .EndsWith(".XML"))
                )
        {
            try
            {
                var xPath = new XPathDocument(assembly.GetManifestResourceStream(name));
                swaggerGenOptions.IncludeXmlComments((Func<XPathDocument>)(() => xPath));
            }
            catch
            {
            }
        }
    }

    if (string.IsNullOrEmpty(directory))
    {
        return;
    }

    foreach (var file in Directory.GetFiles(directory, "*.XML", SearchOption.AllDirectories))
    {
        swaggerGenOptions.IncludeXmlComments(file);
    }
}

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
        Title = "ToDo API",
        Description = "An ASP.NET Core Web API for managing ToDo items",
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
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();