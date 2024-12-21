using System.Globalization;
using I18Next.Net.AspNetCore;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using Lab.i18N.WebApi;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    ;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddI18NextLocalization(i18N =>
{
    i18N.Configure(options =>
    {
        options.DefaultNamespace = "translation";
        options.DefaultLanguage = "de";
        options.FallbackLanguages = new[] { "en" };
    });

    i18N.IntegrateToAspNetCore()
        .AddBackend(new JsonFileBackend("wwwroot/locales"))
        .AddBackend<InMemoryBackend>(p =>
        {
            var memoryBackend = new InMemoryBackend();
            memoryBackend.AddTranslation("en", "translation", "exampleKey", "My English text.");
            memoryBackend.AddTranslation("zh", "translation", "exampleKey", "我的中文字");
            return memoryBackend;
        })
        ;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// var supportedCultures = new[] { "en-US", "de-DE", "zh-TW" };
var supportedCultures = new[] { "en", "de", "zh" };
var cultureInfos = supportedCultures.Select(c => new CultureInfo(c)).ToList();

app.UseRequestLocalization(p =>
{
    p.DefaultRequestCulture = new RequestCulture("zh");
    p.SupportedCultures = cultureInfos;
    p.SupportedUICultures = cultureInfos;
    p.RequestCultureProviders.Insert(0, new HeaderRequestCultureProvider());
});
app.UseStaticFiles();
app.UseHttpsRedirection();
app.MapDefaultControllerRoute();
app.UseRouting();

app.Run();