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

builder.Services.AddHealthChecksUI()
    .AddInMemoryStorage();
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("https://www.google.com1"), "3rd api health check", tags: new[] { "3rd" });
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
app.MapHealthChecks("/_hc",
    new HealthCheckOptions()
    {
        // ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse

        // ResponseWriter = (context, report) =>
        // {
        //     context.Response.ContentType = MediaTypeNames.Text.Plain;
        //     return context.Response.WriteAsync("OK");
        // }

        ResponseWriter = async (context, report) =>
        {
            var result = JsonSerializer.Serialize(
                new
                {
                    status = report.Status.ToString(),
                    errors = report.Entries
                        .Select(e =>
                            new
                            {
                                key = e.Key,
                                value = Enum.GetName(typeof(HealthStatus), e.Value.Status)
                            })
                });
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(result);
        }
    });

app.UseHealthChecksUI(options => { options.UIPath = "/_hc-ui"; });

app.Run();