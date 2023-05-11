using System.Reflection;
using Lab.JsonMergePatch;
using Morcatko.AspNetCore.JsonMergePatch;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(opts => JsonSerializeFactory.Apply(opts.JsonSerializerOptions))
    .AddSystemTextJsonMergePatch(p => p.EnableDelete = true);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(p => p.ExampleFilters());
builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());
builder.Services.AddSingleton(p => JsonSerializeFactory.CreateDefaultOptions());
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