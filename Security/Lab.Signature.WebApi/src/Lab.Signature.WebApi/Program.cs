using Lab.Signature.WebApi.Data;
using Lab.Signature.WebApi.Middleware;
using Lab.Signature.WebApi.Repositories;
using Lab.Signature.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Lab.Signature.WebApi",
        Version = "v1",
        Description = "HMAC-SHA256 簽章保護 API 端點教學示範。呼叫 /api/protected/* 端點時，" +
                      "需帶 X-Api-Key / X-Timestamp / X-Nonce / X-Signature 四個 Header。"
    });

    const string apiKeyHeader = "X-Api-Key";
    const string timestampHeader = "X-Timestamp";
    const string nonceHeader = "X-Nonce";
    const string signatureHeader = "X-Signature";

    options.AddSecurityDefinition(apiKeyHeader, new OpenApiSecurityScheme
    {
        Name = apiKeyHeader,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "合作夥伴的 ApiKey。"
    });
    options.AddSecurityDefinition(timestampHeader, new OpenApiSecurityScheme
    {
        Name = timestampHeader,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Unix epoch seconds，須落在伺服器時間 ±5 分鐘視窗內。"
    });
    options.AddSecurityDefinition(nonceHeader, new OpenApiSecurityScheme
    {
        Name = nonceHeader,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "一次性亂數字串，用於防止 Replay 攻擊，同一 ApiKey 底下不可重複使用。"
    });
    options.AddSecurityDefinition(signatureHeader, new OpenApiSecurityScheme
    {
        Name = signatureHeader,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "對 Canonical String 計算 HMAC-SHA256 後的小寫 hex 字串。"
    });

    var securityRequirement = (OpenApiDocument document) =>
    {
        var requirement = new OpenApiSecurityRequirement();
        foreach (var headerName in new[] { apiKeyHeader, timestampHeader, nonceHeader, signatureHeader })
        {
            requirement.Add(new OpenApiSecuritySchemeReference(headerName, document), new List<string>());
        }

        return requirement;
    };
    options.AddSecurityRequirement(securityRequirement);

    const string adminKeyHeader = "X-Admin-Key";
    options.AddSecurityDefinition(adminKeyHeader, new OpenApiSecurityScheme
    {
        Name = adminKeyHeader,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "內部管理用 Admin Key，僅 Development 環境的 /api/admin/* 端點需要。"
    });
});
builder.Services.AddDbContext<SignatureDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SignatureDb")));
builder.Services.AddScoped<IApiKeyClientRepository, ApiKeyClientRepository>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ISignatureValidationHandler, SignatureValidationHandler>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<INonceStore, MemoryCacheNonceStore>();
builder.Services.AddSingleton<IApiKeyGenerator, ApiKeyGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseMiddleware<AdminAuthenticationMiddleware>();
app.UseMiddleware<SignatureAuthenticationMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}
