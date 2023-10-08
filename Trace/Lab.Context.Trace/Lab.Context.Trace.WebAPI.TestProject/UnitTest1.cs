namespace Lab.Context.Trace.WebAPI.TestProject;

class Data
{
    public string? TraceId { get; set; }
}

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task TestMethod1()
    {
        var server = new TestServer();
        var httpClient = server.CreateDefaultClient();
        
        var url = "https://localhost:7004/demo";

        var tasks = new List<Task<Data>>();
        for (var i = 0; i < 10000; i++)
        {
            tasks.Add(SendAsync(httpClient, url));
        }

        var data = await Task.WhenAll(tasks);

        var duplicateData = data.GroupBy(p => p.TraceId)
            .Where(p => p.Count() > 1)
            .Select(p => p.Key);
        
        foreach (var item in duplicateData)
        {
            Console.WriteLine(item);
        }

        if (duplicateData.Any())
        {
            Assert.Fail("有重複的 trace id");
        }
    }

    static async Task<Data> SendAsync(HttpClient httpClient, string url)
    {
        var response = await httpClient.GetAsync(url);
        response.Headers.TryGetValues("x-trace-id", out var traceIds);
        var traceId = traceIds.FirstOrDefault();
        return new Data()
        {
            TraceId = traceId
        };
    }
}