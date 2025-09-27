var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/members", () =>
{
    return new Member { Name = "小章", Age = 18 };
})
.WithName("GetMembers");

app.Run();

record Member
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
}
