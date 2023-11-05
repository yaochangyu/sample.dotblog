using Lab.Snapshot.DB;
using Lab.Snapshot.WebAPI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => JsonSerializeFactory.Apply(options.JsonSerializerOptions))
    ;

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddScoped<AuthContext>(p => new AuthContext
// {
//     TraceId = Guid.NewGuid().ToString(),
//     Now = DateTimeOffset.UtcNow,
//     UserId = "System-" + Guid.NewGuid().ToString()
// });

builder.Services.AddSingleton<ContextAccessor<AuthContext>>();
builder.Services.AddSingleton<IContextGetter<AuthContext>>(p => p.GetService<ContextAccessor<AuthContext>>());
builder.Services.AddSingleton<IContextSetter<AuthContext>>(p => p.GetService<ContextAccessor<AuthContext>>());

builder.Services.AddSingleton(_ => JsonSerializeFactory.Init());
builder.Services.AddSingleton<MemberRepository>();
builder.Services.AddAutoMapper(typeof(Mapper));
ConfigDb(builder.Services);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var contextSetter = context.RequestServices.GetService<IContextSetter<AuthContext>>();
    var authContext = new AuthContext
    {
        TraceId = Guid.NewGuid().ToString(),
        Now = DateTimeOffset.UtcNow,
        UserId = "System"
    };
    contextSetter.Set(authContext);
    context.Response.Headers.Add("X-Trace-Id", authContext.TraceId);

    await next.Invoke();
});

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

static void ConfigDb(IServiceCollection services)
{
    //依照真實情況注入連線字串
    Environment.SetEnvironmentVariable(EnvironmentNames.DbConnectionString,
        EnvironmentNames.Values[EnvironmentNames.DbConnectionString]);

    services.AddSingleton(p => { return LoggerFactory.Create(builder => { builder.AddConsole(); }); });
    services.AddDbContextFactory<MemberDbContext>((p, options) =>
    {
        //依照真實情況取得連線字串
        var connectionString = Environment.GetEnvironmentVariable(EnvironmentNames.DbConnectionString);
        options.UseNpgsql(connectionString,
                builder => builder.EnableRetryOnFailure(
                    10,
                    TimeSpan.FromSeconds(30),
                    new List<string> { "57P01" }))
            ;
    });
}