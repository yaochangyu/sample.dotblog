using Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.AddConsole();
builder.Services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(BasicAuthenticationDefaults.AuthenticationScheme, null);
builder.Services.AddSingleton<IBasicAuthenticationProvider, BasicAuthenticationProvider>();

// builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, PermissionAuthorizationMiddlewareResultHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}