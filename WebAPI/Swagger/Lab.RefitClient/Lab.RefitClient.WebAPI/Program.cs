using Lab.RefitClient;
using Lab.RefitClient.WebAPI;
using Lab.RefitClient.WebAPI.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(p =>
{
    p.Filters.Add(new ResolverHeaderContextFilterAttribute());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<ContextAccessor<HeaderContext>>();
builder.Services.AddSingleton<IContextSetter<HeaderContext>>(p => p.GetService<ContextAccessor<HeaderContext>>());
builder.Services.AddSingleton<IContextGetter<HeaderContext>>(p => p.GetService<ContextAccessor<HeaderContext>>());

builder.Services.AddScoped<IPetStoreController, PetStoreControllerImpl>();

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

namespace Lab.RefitClient.WebAPI
{
    public partial class Program
    {
    }
}