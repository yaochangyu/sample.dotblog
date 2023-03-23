using System.Net.Http.Headers;
using System.Text;
using Lab.FeatureToggle.WebAPI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var environmentName = builder.Environment.EnvironmentName;
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
builder.Services.AddFeatureManagement();
builder.Services.AddFeatureManagement()
    .UseDisabledFeaturesHandler((features, context) =>
    {
        context.Result = new ObjectResult(new
        {
            FailureCode = "FeatureDisabled",
            FailureMessage = $"The feature {features.First()} is disabled.",
            TraceId = context.HttpContext.TraceIdentifier
        })
        {
            StatusCode = 404
        };
    });
builder.Services.AddFeatureManagement().AddFeatureFilter<PercentageFilter>();
builder.Services.AddFeatureManagement().AddFeatureFilter<DemoFeatureFilter>();

builder.Services.AddControllers(p => p.Filters.AddForFeature<DemoAsyncActionFilter>(FeatureFlags.FeatureB));

// builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.UseForFeature(FeatureFlags.FeatureA, appBuilder =>
{
    appBuilder.Use(async (context, next) =>
    {
        Console.WriteLine("on middleware execution");

        // Do something with the request
        await next.Invoke();

        // Do something with the response
    });
});
app.Run();