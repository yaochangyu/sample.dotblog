using AspNetCore.Authentication.ApiKey;
using Lab.AspNetCore.Security.BasicAuthentication;
using Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    // .AddApiKeyInHeaderOrQueryParams<ApiKeyProvider>(options =>
    // {
    //     options.Realm = "Sample Web API";
    //     options.KeyName = "X-API-KEY";
    // })
    .AddBasicAuthentication<BasicAuthenticationProvider>(BasicAuthenticationDefaults.AuthenticationScheme,
        null,
        o => o.Realm = "Basic Authentication");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();