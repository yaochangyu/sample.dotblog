using System.Net.Mime;
using System.Text.Json;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddHealthChecks();

builder.Services.AddHealthChecksUI(p=>
    {
        p.AddHealthCheckEndpoint("Readiness", "/_readiness");
        p.AddHealthCheckEndpoint("Liveness", "/_liveness");
    })
    .AddInMemoryStorage();

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("https://www.google.com1"), "3rd API", tags: new[] { "3rd API", "google" })
    .AddNpgSql(
        npgsqlConnectionString: "Host=localhost;Port=5432;Database=member_service;Username=postgres;Password=guest",
        healthQuery: "SELECT 1;",
        name: "db",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "PostgreSQL" })
    ;
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
app.MapHealthChecks("/_liveness", new HealthCheckOptions()
{
    Predicate = _ => false, //只檢查應用程式本身
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/_readiness",
    new HealthCheckOptions()
    {
        //檢查應用程式所依賴的服務
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse

        // ResponseWriter = (context, report) =>
        // {
        //     context.Response.ContentType = MediaTypeNames.Text.Plain;
        //     return context.Response.WriteAsync("OK");
        // }

        // ResponseWriter = async (context, report) =>
        // {
        //     var result = JsonSerializer.Serialize(
        //         new
        //         {
        //             status = report.Status.ToString(),
        //             errors = report.Entries
        //                 .Select(e =>
        //                     new
        //                     {
        //                         key = e.Key,
        //                         value = Enum.GetName(typeof(HealthStatus), e.Value.Status)
        //                     })
        //         });
        //     context.Response.ContentType = MediaTypeNames.Application.Json;
        //     await context.Response.WriteAsync(result);
        // }
    });

app.UseHealthChecksUI(options => { options.UIPath = "/_hc"; });

app.Run();