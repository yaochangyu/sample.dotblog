using Lab.LargeObject.Api;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new PooledDoubleArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new PooledMemberAccountArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/api/readings", ([FromBody] PooledArray<double> readings) =>
{
    // 用完務必歸還租用陣列；readings 離開這個 using 區塊前都不能外流出去。
    using (readings)
    {
        var span = readings.Span;
        double sum = 0;
        for (var i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }

        var average = span.Length == 0 ? 0 : sum / span.Length;
        return Results.Ok(new ReadingsSummary(span.Length, sum, average));
    }
});

app.MapPost("/api/members", ([FromBody] PooledArray<MemberAccount> members) =>
{
    // 同樣的規則：租用陣列的使用範圍鎖死在這個 request 處理過程裡，離開前務必歸還。
    using (members)
    {
        var span = members.Span;
        var active = 0;
        var suspended = 0;
        var deleted = 0;

        for (var i = 0; i < span.Length; i++)
        {
            switch (span[i].Status)
            {
                case MemberStatus.Active:
                    active++;
                    break;
                case MemberStatus.Suspended:
                    suspended++;
                    break;
                case MemberStatus.Deleted:
                    deleted++;
                    break;
            }
        }

        return Results.Ok(new MemberAccountSummary(span.Length, active, suspended, deleted));
    }
});

app.Run();

public record ReadingsSummary(int Count, double Sum, double Average);

public record MemberAccountSummary(int Count, int ActiveCount, int SuspendedCount, int DeletedCount);

// 讓 WebApplicationFactory<Program> 能找到進入點型別。
public partial class Program;
