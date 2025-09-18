using Lab.QueueApi.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Queued Web API", 
        Version = "v1",
        Description = "A Web API with rate limiting and queuing mechanism"
    });
    
    // Include XML comments for better API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Register custom services
builder.Services.AddSingleton<IRateLimiter>(provider => 
    new SlidingWindowRateLimiter(maxRequests: 2, timeWindow: TimeSpan.FromSeconds(10)));

builder.Services.AddSingleton<IQueueHandler>(provider => 
    new ChannelQueueHandler(capacity: 100));

// Register RequestProcessorService as both singleton and hosted service
builder.Services.AddSingleton<RequestProcessorService>();

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Queued Web API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/", () => new
{
    Service = "Queued Web API",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Documentation = "/swagger"
});

app.Run();
