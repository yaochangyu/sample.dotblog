using Lab.LargeObject.Api;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new PooledDoubleArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new PooledStringArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new PooledMemberAccountArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new PooledMemberAccountClassArrayJsonConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/diag/gc-stats", () =>
{
    var memInfo = GC.GetGCMemoryInfo();
    return Results.Ok(new
    {
        TotalPauseDurationMs = GC.GetTotalPauseDuration().TotalMilliseconds,
        PauseTimePercentage = memInfo.PauseTimePercentage,
        Gen0Collections = GC.CollectionCount(0),
        Gen1Collections = GC.CollectionCount(1),
        Gen2Collections = GC.CollectionCount(2)
    });
});

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

// ==========================================
// 4. 原生字串陣列 (string[]) 端點
// ==========================================

app.MapPost("/api/strings-list", ([FromBody] List<string> strings) =>
{
    // 未池化 List<string>
    long totalLength = 0;
    for (var i = 0; i < strings.Count; i++)
    {
        if (strings[i] is not null)
        {
            totalLength += strings[i].Length;
        }
    }
    return Results.Ok(new StringsSummary(strings.Count, totalLength));
});

app.MapPost("/api/strings", ([FromBody] PooledArray<string> strings) =>
{
    // ArrayPool 池化 string[]
    using (strings)
    {
        var span = strings.Span;
        long totalLength = 0;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] is not null)
            {
                totalLength += span[i].Length;
            }
        }
        return Results.Ok(new StringsSummary(span.Length, totalLength));
    }
});

app.MapPost("/api/strings-stream", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    // IAsyncEnumerable<string> 串流解析
    var count = 0;
    long totalLength = 0;

    await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<string>(request.Body, cancellationToken: cancellationToken))
    {
        if (item is not null)
        {
            count++;
            totalLength += item.Length;
        }
    }

    return Results.Ok(new StringsSummary(count, totalLength));
});

// ==========================================
// 5. 大型 Response（回傳資料）端點情境
// ==========================================

// 1. ❌ List 未池化：在記憶體中建立 20,000 筆的大 List 直接 Results.Ok
app.MapGet("/api/export-list", () =>
{
    var list = new List<MemberAccount>(20000);
    for (var i = 0; i < 20000; i++)
    {
        var status = (i % 3) switch
        {
            0 => MemberStatus.Active,
            1 => MemberStatus.Suspended,
            _ => MemberStatus.Deleted
        };
        list.Add(new MemberAccount
        {
            MemberId = i,
            Account = $"member{i:D6}",
            DisplayName = $"會員 {i}",
            Status = status,
            Contact = new ContactInfo
            {
                Email = $"member{i:D6}@example.com",
                PhoneNumber = (i % 2 == 0) ? $"09{i:D8}" : null
            },
            CreatedAt = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc)
        });
    }
    return Results.Ok(list);
});

// 2. ❌ SerializeToUtf8Bytes / byte[]：直接序列化為大型 byte 陣列放進 LOH
app.MapGet("/api/export-bytes", () =>
{
    var list = new List<MemberAccount>(20000);
    for (var i = 0; i < 20000; i++)
    {
        var status = (i % 3) switch
        {
            0 => MemberStatus.Active,
            1 => MemberStatus.Suspended,
            _ => MemberStatus.Deleted
        };
        list.Add(new MemberAccount
        {
            MemberId = i,
            Account = $"member{i:D6}",
            DisplayName = $"會員 {i}",
            Status = status,
            Contact = new ContactInfo
            {
                Email = $"member{i:D6}@example.com",
                PhoneNumber = (i % 2 == 0) ? $"09{i:D8}" : null
            },
            CreatedAt = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc)
        });
    }

    var bytes = JsonSerializer.SerializeToUtf8Bytes(list, new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() }
    });

    return Results.File(bytes, "application/json");
});

// 3. ⚡ ArrayPool 池化：租用 Buffer 填充後序列化輸出至 Response.Body
app.MapGet("/api/export-pooled", async (HttpResponse response, CancellationToken ct) =>
{
    var rented = System.Buffers.ArrayPool<MemberAccount>.Shared.Rent(20000);
    try
    {
        for (var i = 0; i < 20000; i++)
        {
            var status = (i % 3) switch
            {
                0 => MemberStatus.Active,
                1 => MemberStatus.Suspended,
                _ => MemberStatus.Deleted
            };
            rented[i] = new MemberAccount
            {
                MemberId = i,
                Account = $"member{i:D6}",
                DisplayName = $"會員 {i}",
                Status = status,
                Contact = new ContactInfo
                {
                    Email = $"member{i:D6}@example.com",
                    PhoneNumber = (i % 2 == 0) ? $"09{i:D8}" : null
                },
                CreatedAt = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        using var pooled = new PooledArray<MemberAccount>(rented, 20000);
        response.ContentType = "application/json";
        var options = new JsonSerializerOptions
        {
            Converters = { new PooledMemberAccountArrayJsonConverter(), new JsonStringEnumConverter() }
        };
        await JsonSerializer.SerializeAsync(response.Body, pooled, options, cancellationToken: ct);
    }
    finally
    {
        System.Buffers.ArrayPool<MemberAccount>.Shared.Return(rented, clearArray: true);
    }
});

// 4. 🏆 IAsyncEnumerable 串流回傳：逐筆 yield return 邊產邊傳，全程 0 LOH
app.MapGet("/api/export-stream", (CancellationToken ct) => StreamMembersAsync(ct));

static async IAsyncEnumerable<MemberAccount> StreamMembersAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
{
    for (var i = 0; i < 20000; i++)
    {
        ct.ThrowIfCancellationRequested();
        var status = (i % 3) switch
        {
            0 => MemberStatus.Active,
            1 => MemberStatus.Suspended,
            _ => MemberStatus.Deleted
        };

        yield return new MemberAccount
        {
            MemberId = i,
            Account = $"member{i:D6}",
            DisplayName = $"會員 {i}",
            Status = status,
            Contact = new ContactInfo
            {
                Email = $"member{i:D6}@example.com",
                PhoneNumber = (i % 2 == 0) ? $"09{i:D8}" : null
            },
            CreatedAt = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}

app.Run();

public record ReadingsSummary(int Count, double Sum, double Average);

public record MemberAccountSummary(int Count, int ActiveCount, int SuspendedCount, int DeletedCount);

public record StringsSummary(int Count, long TotalLength);

// 讓 WebApplicationFactory<Program> 能找到進入點型別。
public partial class Program;
