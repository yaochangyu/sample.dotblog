using Lab.RefitClient;
using Lab.RefitClient.GeneratedCode.PetStore;
using Lab.RefitClient.WebAPI;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(p => p.Filters.Add<GenHeaderContextFilterAttribute>());

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ContextAccessor<HeaderContext>>();
builder.Services.AddSingleton<IContextSetter<HeaderContext>>(p => p.GetService<ContextAccessor<HeaderContext>>());
builder.Services.AddSingleton<IContextGetter<HeaderContext>>(p => p.GetService<ContextAccessor<HeaderContext>>());

var baseUrl = "https://localhost:7285/api/v3";

builder.Services.AddSingleton(p =>
{
    var settings = new RefitSettings
    {
        HttpMessageHandlerFactory = () =>
        {
            var contextGetter = p.GetService<IContextGetter<HeaderContext>>();
            return new DefaultHeaderHandler(contextGetter)
            {
                InnerHandler = new SocketsHttpHandler()
            };
        },
    };
    return settings;
});

builder.Services
    .AddRefitClient<ISwaggerPetstoreOpenAPI30>(p => p.GetRequiredService<RefitSettings>())
    .ConfigureHttpClient(p => { p.BaseAddress = new Uri(baseUrl); })
    ;

// builder.Services
//     .AddRefitClient<ISwaggerPetstoreOpenAPI30>()
//     .ConfigureHttpClient(p => { p.BaseAddress = new Uri(baseUrl); })
//     .AddHttpMessageHandler(p => new DefaultHeaderHandler(p.GetService<IContextGetter<HeaderContext>>()))
//     ;
var app = builder.Build();

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