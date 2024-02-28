using System.Net;
using System.Text;
using System.Text.Json.JsonDiffPatch;
using DotNet.Testcontainers.Builders;
using FluentAssertions;

namespace Lab.MockServer.Test;

public class UnitTest2
{
    [Fact]
    public async Task 動態建立假端點_TestContainers()
    {
        //建立假的端點
        var url = "mockserver/expectation";
        var body = """
                   {
                     "httpRequest": {
                       "method": "GET",
                       "path": "/view/cart"
                     },
                     "httpResponse": {
                       "body": "some_response_body"
                     }
                   }
                   """;

        var container = new ContainerBuilder()
            .WithImage("mockserver/mockserver")
            .WithPortBinding(1080, assignRandomHostPort: true)
            .Build();
        await container.StartAsync();
        var hostname = container.Hostname;
        var port = container.GetMappedPublicPort(1080);
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{hostname}:{port}/")
        };
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = httpClient.SendAsync(request).Result;
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        //呼叫假的端點
        var getCartResult = httpClient.GetStringAsync("view/cart?cartId=055CA455-1DF7-45BB-8535-4F83E7266092").Result;
        getCartResult.Should().Be("some_response_body");
    }
}