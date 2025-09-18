using Lab.QueueApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 註冊排隊機制服務
builder.Services.AddSingleton<IRateLimitService>(sp =>
    new SlidingWindowRateLimiter(maxRequests: 2, timeWindow: TimeSpan.FromMinutes(1)));
builder.Services.AddSingleton<IRequestPool, RequestPool>();
builder.Services.AddScoped<IRequestProcessor, RequestProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }
