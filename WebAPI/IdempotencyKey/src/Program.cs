using IdempotencyKey.WebApi.IdempotencyKeys;
using IdempotencyKey.WebApi.Members;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<MemberDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMemberRepository, EfMemberRepository>();
builder.Services.AddScoped<IIdempotencyKeyStore, EfIdempotencyKeyStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
