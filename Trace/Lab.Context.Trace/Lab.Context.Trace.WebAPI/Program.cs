using Lab.Context.Trace;
using Lab.Context.Trace.WebAPI;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/host-.txt", rollingInterval: RollingInterval.Hour)
    .CreateLogger();
Log.Information("Starting web host");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllers();
    
    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .WriteTo.File("logs/aspnet-.txt", rollingInterval: RollingInterval.Minute)
    );

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSingleton<ContextAccessor<TraceContext>>();
    builder.Services.AddSingleton<IContextGetter<TraceContext>>(p => p.GetService<ContextAccessor<TraceContext>>());
    builder.Services.AddSingleton<IContextSetter<TraceContext>>(p => p.GetService<ContextAccessor<TraceContext>>());
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseHttpsRedirection();

    app.UseAuthorization();
    app.UseMiddleware<TraceContextMiddleware>();

    app.MapControllers();
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}