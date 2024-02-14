using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WebAppA.Controllers;

public class LabService : ILabService
{
    private readonly HttpClient _client;

    public LabService(HttpClient client)
    {
        this._client = client;
    }

    public IEnumerable<string> Get()
    {
        var url = "api/default";
        var response = this._client.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var result = JsonSerializer.Deserialize<string[]>(content);

        return result;
    }
}

public interface ILabService
{
    IEnumerable<string> Get();
}

[ApiController]

// [Route("[controller]")]
public class Demo1Controller : ControllerBase
{
    private readonly ILogger<Demo1Controller> _logger;
    private readonly HttpClient _client;

    private readonly HttpClient s_client = new HttpClient
    {
        BaseAddress = new Uri(serverUrl),
    };

    private readonly IHttpClientFactory _httpClientFactory;
    static string serverUrl = "https://localhost:7004/";
    private ILabService _service;

    // public Demo1Controller(ILogger<Demo1Controller> logger, ILabService service)
    // {
    //     this._logger = logger;
    //     this._service = service;
    // }

    public Demo1Controller(ILogger<Demo1Controller> logger, IHttpClientFactory httpClientFactory)
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    [Route("/api/demo1")]
    public async Task<ActionResult> Get()
    {
        var requestId = this.HttpContext.TraceIdentifier;

        //header
        if (this.Request.Headers.Any())
        {
            this._logger.LogInformation("1.Id={@RequestId}, At 'Demo1', Receive request headers ={@Headers}", requestId,
                this.Request.Headers);
        }

        //cookie
        if (this.Request.Cookies.Any())
        {
            this._logger.LogInformation("2.Id={@RequestId}, At 'Demo1', Receive request cookie ={@Cookies}", requestId,
                this.Request.Cookies);
        }

        var url = "api/Demo2";

        // var client = this._client;

        // var client = this.s_client;

        var client = this._httpClientFactory.CreateClient("lab");

        // var client = new HttpClient()
        // {
        //     BaseAddress = new Uri(serverUrl)
        // };

        //送出並行請求
        // await SendMultiRequest(client, url);

        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers =
            {
                { "A", "123" },
            }
        };
        var response = await client.SendAsync(request);

        if (response.Headers.Any())
        {
            this._logger.LogInformation("3.Id={@RequestId}, At 'Demo1', Receive response headers ={@Headers}",
                requestId,
                response.Headers);
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseContent) == false)
        {
            this._logger.LogInformation("4.Id={@RequestId}, At 'Demo1', Receive response body ={@Body}",
                requestId,
                responseContent);
        }
     
        // if (response.Headers.TryGetValues("A", out var a2))
        // {
        //     this._logger.LogInformation("Id={RequestId},Response Header 'A'={Data}", requestId, a2);
        // }
        //
        // if (response.Headers.TryGetValues("B", out var b2))
        // {
        //     this._logger.LogInformation("Id={RequestId},Response Header 'B'={Data}", requestId, b2);
        // }
        //
        // if (response.Headers.TryGetValues("C", out var c2))
        // {
        //     this._logger.LogInformation("Id={RequestId},Response Header 'C'={Data}", requestId, c2);
        // }
        //
        // if (response.Headers.TryGetValues("D", out var d2))
        // {
        //     this._logger.LogInformation("Id={RequestId},Response Header 'D'={Data}", requestId, d2);
        // }
        //
        // //取得 Response Cookies
        // if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        // {
        //     this._logger.LogInformation("Id={RequestId},Response Cookie 'All Cookie'={Data}", requestId, cookies);
        // }
        return this.Ok();
    }

    private static async Task SendParallelRequest(HttpClient client, string url)
    {
        var processTime = new Stopwatch();
        processTime.Start();
        var tasks = new List<Task>();
        while (true)
        {
            //送出並行請求
            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers =
                {
                    { "A", "123" },
                }
            };

            var task = client.SendAsync(request);
            tasks.Add(task);
            if (processTime.Elapsed.TotalSeconds > 5)
            {
                //五秒後就停止
                processTime.Stop();
                break;
            }
        }

        await Task.WhenAll(tasks);
    }
}