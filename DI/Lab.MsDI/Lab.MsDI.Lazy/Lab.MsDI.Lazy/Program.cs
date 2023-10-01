using Lab.MsDI.Lazy;
using LazyProxy;
using LazyProxy.ServiceProvider;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddScoped<IServiceA, ServiceA>();
// builder.Services.AddScoped<IServiceB, ServiceB>();

// builder.Services.AddScoped<IService, Service>();
//
// builder.Services.AddScoped<IService>(p =>
// {
//     var serviceA = new Lazy<IServiceA>(() => p.GetService<IServiceA>());
//     var serviceB = new Lazy<IServiceB>(() => p.GetService<IServiceB>());
//     return new ServiceLazy(serviceA, serviceB);
// });

// builder.Services.AddScoped<IService>(p =>
// {
//     var lazyProxy = LazyProxyBuilder.CreateInstance<IService>(() =>
//     {
//         var serviceA = p.GetService<IServiceA>();
//         var serviceB = p.GetService<IServiceB>();
//         return new Service(serviceA, serviceB);
//     });
//     return lazyProxy;
// });

builder.Services.AddLazyScoped<IServiceA, ServiceA>();
builder.Services.AddLazyScoped<IServiceB, ServiceB>();
builder.Services.AddLazyScoped<IService, Service>();

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