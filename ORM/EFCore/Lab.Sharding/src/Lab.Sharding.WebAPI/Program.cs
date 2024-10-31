using Lab.Sharding.DB;
using Lab.Sharding.Infrastructure;
using Lab.Sharding.WebAPI;
using Lab.Sharding.WebAPI.Member;
using Scalar.AspNetCore;
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
	if (Array.FindIndex(args, x => x == "--local") >= 0)
	{
		var envFolder = EnvironmentUtility.FindParentFolder("env");
		EnvironmentUtility.ReadEnvironmentFile(envFolder, "local.env");
	}

	var builder = WebApplication.CreateBuilder(args);

	// Add services to the container.
	builder.Services.AddSingleton(p => JsonSerializeFactory.DefaultOptions);
	builder.Services.AddControllers()
		.AddJsonOptions(options => JsonSerializeFactory.Apply(options.JsonSerializerOptions))
		;
	builder.Host
		.UseSerilog((context, services, config) =>
						config.ReadFrom.Configuration(context.Configuration)
							.ReadFrom.Services(services)
							.Enrich.FromLogContext()
							.WriteTo.Console() //正式環境不要用 Console，除非有 Log Provider 專門用來收集 Console Log
							.WriteTo.Seq("http://localhost:5341") //log server
							.WriteTo.File("logs/aspnet-.txt", rollingInterval: RollingInterval.Minute) //正式環境不要用 File
		);

	// 確定物件都有設定 DI Container
	builder.Host.UseDefaultServiceProvider(p =>
	{
		p.ValidateScopes = true;
		p.ValidateOnBuild = true;
	});
	var configuration = builder.Configuration;

	builder.Services.AddStackExchangeRedisCache(options =>
	{
		var connectionString = configuration.GetValue<string>(nameof(SYS_REDIS_URL));

		// options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
		// {
		//     EndPoints = { connectionString },
		//     DefaultDatabase = 0,
		// };

		options.Configuration = connectionString;

		// options.InstanceName = "SampleInstance";
	});

	// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();
	builder.Services.AddHttpContextAccessor();

	builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
	builder.Services.AddContextAccessor();
	builder.Services.AddSysEnvironments();
	builder.Services.AddScoped<IUuidProvider, UuidProvider>();
	builder.Services.AddScoped<MemberHandler>();
	builder.Services.AddScoped<MemberRepository>();
	builder.Services.AddExternalApiHttpClient();
	builder.Services.AddDatabase();

	var app = builder.Build();

	// Configure the HTTP request pipeline.
	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI(options =>
							 options.SwaggerEndpoint("/swagger/v1/swagger.yaml",
													 "Swagger Demo Documentation v1"));
		app.UseReDoc(options =>
		{
			options.DocumentTitle = "Swagger Demo Documentation";
			options.SpecUrl = "/swagger/v1/swagger.yaml";
			options.RoutePrefix = "redoc";
			options.ConfigObject.HideHostname = true;
		});

		app.MapScalarApiReference(p =>
		{
			p.OpenApiRoutePattern = "/swagger/v1/swagger.json";

			// p.EndpointPathPrefix = "scalar";
		});
	}

	app.UseAuthorization();
	app.UseMiddleware<TraceContextMiddleware>();
	app.MapDefaultControllerRoute();
	app.UseRouting();
	app.UseEndpoints(endpoints =>
	{
		//注册Web API Controller
		endpoints.MapControllers();

		//注册MVC Controller模板 {controller=Home}/{action=Index}/{id?}
		// endpoints.MapDefaultControllerRoute();

		//注册健康检查
		// endpoints.MapHealthChecks("/_hc");
	});
	app.UseSerilogRequestLogging();
	app.UseHttpsRedirection();
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

namespace Lab.Sharding.WebAPI
{
	public class Program
	{
	}
}