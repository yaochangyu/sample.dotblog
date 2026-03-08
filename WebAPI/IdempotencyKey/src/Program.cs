using IdempotencyKey.WebApi;
using IdempotencyKey.WebApi.IdempotencyKeys;
using IdempotencyKey.WebApi.Members;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options => JsonSerializeFactory.Apply(options.JsonSerializerOptions))
    ;

builder.Services.AddDbContext<MemberDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddScoped<IMemberRepository, EfMemberRepository>();
builder.Services.AddScoped<MemberHandler>();
builder.Services.AddSingleton<IIdempotencyKeyStore, RedisIdempotencyKeyStore>();
builder.Services.AddSingleton(p => JsonSerializeFactory.DefaultOptions);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();