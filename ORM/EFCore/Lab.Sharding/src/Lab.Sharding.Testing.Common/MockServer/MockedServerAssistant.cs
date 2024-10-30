using System.Text;
using System.Text.Json;
using Lab.Sharding.Testing.Common.MockServer.Contracts;

namespace Lab.Sharding.Testing.Common.MockServer;

public class MockedServerAssistant
{
    public static async Task ResetAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"mockserver/reset");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public static async Task PutNewEndPointAsync(HttpClient client, 
                                          string httpMethod, 
                                          string relativePath,
                                          int statusCode,
                                          string body)
    {
        var requestModel = new PutNewEndPointRequest
        {
            HttpRequest = new HttpRequest
            {
                Method = httpMethod.ToUpper(),
                Path = relativePath.StartsWith("/") ? relativePath : $"/{relativePath}",
            },
            HttpResponse = new HttpResponse { Body = body, StatusCode = statusCode }
        };

        var content = JsonSerializer.Serialize(
            requestModel,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, });

        using var request = new HttpRequestMessage(HttpMethod.Put, $"mockserver/expectation")
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }
}