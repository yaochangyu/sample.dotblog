using Lab.LargeObject.Api;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new PooledDoubleArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new PooledMemberAccountArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new PooledMemberAccountClassArrayJsonConverter());
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

app.MapPost("/api/readings-list", ([FromBody] List<double> readings) =>
{
    // 未使用 ArrayPool，System.Text.Json 會直接建立 List<double> 並動態擴容，
    // 當元素數量達到 10,625 個 double (或在擴容至 16,384 時) 超過 85,000 bytes 門檻，
    // 底層陣列將直接配置於 LOH。
    double sum = 0;
    for (var i = 0; i < readings.Count; i++)
    {
        sum += readings[i];
    }

    var average = readings.Count == 0 ? 0 : sum / readings.Count;
    return Results.Ok(new ReadingsSummary(readings.Count, sum, average));
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

app.MapPost("/api/members-list", ([FromBody] List<MemberAccount> members) =>
{
    // 未使用 ArrayPool 的複雜型別端點。
    // 每個 MemberAccount struct 為 64 bytes，當元素數量達到 1,329 個 (或擴容至 2,048 時)
    // 內部陣列超過 85,000 bytes 門檻，將直接落在 LOH。
    var active = 0;
    var suspended = 0;
    var deleted = 0;

    for (var i = 0; i < members.Count; i++)
    {
        switch (members[i].Status)
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

    return Results.Ok(new MemberAccountSummary(members.Count, active, suspended, deleted));
});

app.MapPost("/api/readings-stream", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    // 使用 IAsyncEnumerable<T> 串流反序列化（System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable）
    // 邊從 HTTP Request Body 讀取串流邊解析與計算，完全不需要在記憶體中配置大陣列容器，
    // 因此 LOH 配置量為 0，且記憶體佔用極低且固定。
    var count = 0;
    double sum = 0;

    await foreach (var reading in JsonSerializer.DeserializeAsyncEnumerable<double>(request.Body, cancellationToken: cancellationToken))
    {
        count++;
        sum += reading;
    }

    var average = count == 0 ? 0 : sum / count;
    return Results.Ok(new ReadingsSummary(count, sum, average));
});

app.MapPost("/api/members-stream", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    // 複雜型別串流反序列化
    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    var count = 0;
    var active = 0;
    var suspended = 0;
    var deleted = 0;

    await foreach (var member in JsonSerializer.DeserializeAsyncEnumerable<MemberAccount>(request.Body, serializerOptions, cancellationToken: cancellationToken))
    {
        count++;
        switch (member.Status)
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

    return Results.Ok(new MemberAccountSummary(count, active, suspended, deleted));
});

app.MapPost("/api/members-class-list", ([FromBody] List<MemberAccountClass> members) =>
{
    // Class + List 版本
    // 每個元素是獨立 class 物件 (Gen0)，List 內部指標陣列在 10,625 個元素後進 LOH
    var active = 0;
    var suspended = 0;
    var deleted = 0;

    for (var i = 0; i < members.Count; i++)
    {
        switch (members[i].Status)
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

    return Results.Ok(new MemberAccountSummary(members.Count, active, suspended, deleted));
});

app.MapPost("/api/members-class-pooled", ([FromBody] PooledArray<MemberAccountClass> members) =>
{
    // Class + ArrayPool 版本
    // ArrayPool 僅池化了指標陣列 (MemberAccountClass[])，
    // 但 20,000 個 MemberAccountClass 與 20,000 個 ContactInfoClass 物件仍各自 new 在 Heap 上。
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

app.MapPost("/api/members-class-stream", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    // Class + 串流版本
    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    var count = 0;
    var active = 0;
    var suspended = 0;
    var deleted = 0;

    await foreach (var member in JsonSerializer.DeserializeAsyncEnumerable<MemberAccountClass>(request.Body, serializerOptions, cancellationToken: cancellationToken))
    {
        if (member is null) continue;
        count++;
        switch (member.Status)
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

    return Results.Ok(new MemberAccountSummary(count, active, suspended, deleted));
});

app.Run();

public record ReadingsSummary(int Count, double Sum, double Average);

public record MemberAccountSummary(int Count, int ActiveCount, int SuspendedCount, int DeletedCount);

// 讓 WebApplicationFactory<Program> 能找到進入點型別。
public partial class Program;
