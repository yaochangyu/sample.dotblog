using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Lab.ErrorHandler.API;
using Lab.ErrorHandler.API.Filters;
using Lab.ErrorHandler.API.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllers(p =>
    {
        // p.Filters.Add<ModelValidationAttribute>();

        // p.ModelValidatorProviders.Clear();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.MaxDepth = 10;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.AllowInputFormatterExceptionMessages = true;
    })
    ;
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    //停用 Model State Invalid Filter
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<MemberService1>();
builder.Services.AddScoped<MemberService2.MemberWorkflow>();
builder.Services.AddScoped<MemberService2>();
builder.Services.AddScoped<MemberService3>();
builder.Services.AddScoped<MemberService4>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/aspnet-.txt", rollingInterval: RollingInterval.Hour)
);

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