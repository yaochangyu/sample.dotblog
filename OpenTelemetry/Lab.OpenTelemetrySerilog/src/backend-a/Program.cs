using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



app.MapGet("/weatherforecast", async (HttpClient client) =>
{
    try
    {
        var response = await client.GetStringAsync("http://localhost:5200/weatherforecast");
        return Results.Content(response, "application/json");
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Error calling Backend-B: {ex.Message}");
    }
})
.WithName("GetWeatherForecast");

app.Run();
