// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

Console.WriteLine("Starting request...");

using (var httpClient = new HttpClient())
{
    var url = $"https://localhost:7004/demo";

    var tasks = new List<Task<Data>>();
    for (var i = 0; i < 2; i++)
    {
        tasks.Add(SendAsync(httpClient, url));
    }

    var data = await Task.WhenAll(tasks);

    // 列出重複的 trace id
    var duplicateData = data.GroupBy(p => p?.TraceId)
        .Where(p => p?.Count() > 1)
        .Where(p => string.IsNullOrWhiteSpace(p?.Key) == false)
        .Select(p => p?.Key);

    var items = new List<string>();

    foreach (var item in duplicateData)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            continue;
        }

        items.Add(item);
        Console.WriteLine(item);
    }

    if (items.Any())
    {
        Debug.Fail("有重複的 trace id");
    }

    Console.WriteLine("All requests completed.");
}

static async Task<Data> SendAsync(HttpClient httpClient, string url)
{
    try
    {
        var response = await httpClient.GetAsync(url);
        response.Headers.TryGetValues("x-trace-id", out var traceIds);
        var traceId = traceIds.FirstOrDefault();
        return new Data()
        {
            TraceId = traceId
        };
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }

    return null;
}

class Data
{
    public string TraceId { get; set; }
}