using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lab.ModelValidation.API;
using Lab.ModelValidation.API.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers(p =>
    {
        // p.Filters.Add<DataValidationAttribute>();
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
    //停用 Model Validation
    options.SuppressModelStateInvalidFilter = false;

    options.InvalidModelStateResponseFactory = actionContext => ValidationErrorHandler(options, actionContext);
});

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

app.Run();

IActionResult ValidationErrorHandler(ApiBehaviorOptions apiBehaviorOptions, ActionContext actionContext)
{
    var originalFactory = apiBehaviorOptions.InvalidModelStateResponseFactory;
    if (actionContext.ModelState.IsValid)
    {
        return originalFactory(actionContext);
    }

    var traceId = Activity.Current?.Id ?? actionContext.HttpContext.TraceIdentifier;

    //處理 JSON Path
    var jsonPathKeys = actionContext.ModelState.Keys.Where(e => e.StartsWith("$.")).ToList();
    if (jsonPathKeys.Count > 0)
    {
        var errorData = new Dictionary<string, string>();
        foreach (var key in jsonPathKeys)
        {
            var normalizedKey = key.Substring(2);
            foreach (var error in actionContext.ModelState[key].Errors)
            {
                if (error.Exception != null)
                {
                    actionContext.ModelState.TryAddModelException(normalizedKey, error.Exception);
                }

                actionContext.ModelState.TryAddModelError(normalizedKey, "The provided value is not valid.");
                errorData.Add(normalizedKey, error.ErrorMessage);
            }

            actionContext.ModelState.Remove(key);
        }

        //複寫錯誤內容
        return new BadRequestObjectResult(new Failure
        {
            Code = FailureCode.InputInvalid,
            Message = "enum invalid",
            Data = errorData,
            TraceId = traceId
        });
    }

    var errors = actionContext.ModelState.ToDictionary(
        p => p.Key,
        p => p.Value.Errors.Select(e => e.ErrorMessage).ToList());

    //複寫錯誤內容
    return new BadRequestObjectResult(new Failure()
    {
        Code = FailureCode.InputInvalid,
        Message = "input invalid",
        Data = errors,
        TraceId = traceId
    });
}