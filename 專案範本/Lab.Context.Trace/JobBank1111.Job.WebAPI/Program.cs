using JobBank1111.Infrastructure.TraceContext;
using JobBank1111.Job.WebAPI;
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
 
    // builder.Services.AddScoped<ContextAccessor<AuthContext>>();
    // builder.Services.AddScoped<IContextGetter<AuthContext>>(p => p.GetService<ContextAccessor<AuthContext>>());
    // builder.Services.AddScoped<IContextSetter<AuthContext>>(p => p.GetService<ContextAccessor<AuthContext>>());
    
    builder.Services.AddSingleton<ContextAccessor<AuthContext>>();
    builder.Services.AddSingleton<IContextGetter<AuthContext>>(p => p.GetService<ContextAccessor<AuthContext>>());
    builder.Services.AddSingleton<IContextSetter<AuthContext>>(p => p.GetService<ContextAccessor<AuthContext>>());
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseSerilogRequestLogging();
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

public partial class Program { }
