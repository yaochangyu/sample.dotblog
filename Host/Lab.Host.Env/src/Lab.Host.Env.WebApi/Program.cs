var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var environmentName = builder.Environment.EnvironmentName;
var configRoot = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
    .Build();

builder.Configuration.AddConfiguration(configRoot);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var version = app.Configuration.GetSection("Extension:Version").Value;
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Extension.Version: {version}");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();