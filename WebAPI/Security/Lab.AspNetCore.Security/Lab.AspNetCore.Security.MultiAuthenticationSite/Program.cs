using Lab.AspNetCore.Security.MultiAuthenticationSite.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
    .AddBasic<BasicAuthenticationProvider>(o => { o.Realm = "My App"; });

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

// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();