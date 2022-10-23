using AspNetCore.Authentication.ApiKey;
using Lab.AspNetCore.Security.BasicAuthentication;
using Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var multiScheme = "MultiAuthSchemes";
builder.Services.AddAuthentication(p =>
    {
        p.DefaultScheme = multiScheme;
        p.DefaultChallengeScheme = multiScheme;
    })
    .AddApiKeyInHeaderOrQueryParams<ApiKeyProvider>(p =>
    {
        p.Realm = "Sample Web API";
        p.KeyName = "X-API-KEY";
    })
    .AddBasicAuthentication<BasicAuthenticationProvider>(BasicAuthenticationDefaults.AuthenticationScheme,
        p =>
        {
            p.Realm = "Basic Authentication";
        })
    .AddPolicyScheme(multiScheme, ApiKeyDefaults.AuthenticationScheme, p =>
    {
        p.ForwardDefaultSelector = context =>
        {
            string authorization = context.Request.Headers[HeaderNames.Authorization];
            if (string.IsNullOrEmpty(authorization) == false &&
                authorization.StartsWith($"{BasicAuthenticationDefaults.AuthenticationScheme} ",
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return BasicAuthenticationDefaults.AuthenticationScheme;
            }

            return ApiKeyDefaults.AuthenticationScheme;
        };
    })
    ;

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