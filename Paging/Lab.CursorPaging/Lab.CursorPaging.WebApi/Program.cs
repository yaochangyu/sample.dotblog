using Lab.CursorPaging.WebApi.Member.Repository;
using Microsoft.EntityFrameworkCore;

Environment.SetEnvironmentVariable("DbConnectionString", "Host=localhost;Port=5432;Database=Member;Username=postgres;Password=guest;");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContextFactory<MemberDbContext>((sp, options) =>
{
    // var connProvider = sp.GetRequiredService<IConnectionProvider>();
    var connString = Environment.GetEnvironmentVariable("DbConnectionString");
    options
        .UseNpgsql(
            connString,
            builder => builder.EnableRetryOnFailure(
                10,
                TimeSpan.FromSeconds(30),
                new List<string> { "57P01" }))
        .UseLoggerFactory(sp.GetService<ILoggerFactory>())
        ;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();