using System.Diagnostics;
using System.Reflection;
using Lab.NETMiniProfiler.Infrastructure.EFCore6;
using Lab.NETMiniProfiler.Infrastructure.EFCore6.EntityModel;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMiniProfiler(o => o.RouteBasePath = "/profiler")
       .AddEntityFramework();
builder.Services.AddAppEnvironment();
builder.Services.AddEntityFramework();
var app = builder.Build();
PreConnectionDb(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    // app.UseSwaggerUI();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.IndexStream = () => typeof(Program).GetTypeInfo()
                                             .Assembly
                                             .GetManifestResourceStream("Lab.NETMiniProfiler.ASPNetCore6.index.html");
    });
    app.UseMiniProfiler();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.Run();

static void PreConnectionDb(IApplicationBuilder app)
{
    var employeeDbContextFactory =
        app.ApplicationServices.GetService<IDbContextFactory<EmployeeDbContext>>();
    var db = employeeDbContextFactory.CreateDbContext();
    if (db.Database.CanConnect())
    {
        Debug.WriteLine("資料庫已連線");
    }
}