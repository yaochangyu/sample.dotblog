// using System.Text;
// using System.Text.Json;
// using DotNet.Testcontainers.Builders;
// using DotNet.Testcontainers.Containers;
// using Lab.Sharding.Testing.Common.MockServers.Contracts;
//
// namespace Lab.Sharding.Testing.Common.MockServers;
//
// public static class MockServerProvider
// {
//     private const int internalPort = 1080;
//     public static int Port => 55124;
//     private static string Image => "mockserver/mockserver";
//     public static string Hostname => $"http://{_container.Value.Hostname}:{Port}";
//
//     public static IContainer CreateContainer() => _container.Value;
//
//     private static Lazy<IContainer> _container = new(() => new ContainerBuilder().WithImage(Image).WithPortBinding(Port,internalPort).Build());
//     private static Lazy<HttpClient> _httpClient = new(() => new HttpClient
//     {
//         BaseAddress = new Uri(Hostname)
//     });
//
//     public static async Task PutNewEndPoint(string httpMethod, string relativePath, int statusCode, string source)
//     {
//         
//         var client = _httpClient.Value!;
//
//         var requestModel = new PutNewEndPointRequest
//         {
//             HttpRequest = new HttpRequest
//             {
//                 Method = httpMethod.ToUpper(),
//                 Path = relativePath.StartsWith("/") ? relativePath : $"/{relativePath}",
//             },
//             HttpResponse =new HttpResponse
//             {
//                 Body = source,
//                 StatusCode = statusCode
//             }
//         };
//         
//         var content = JsonSerializer.Serialize(
//             requestModel,
//             new JsonSerializerOptions
//         {
//         
//             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//         });
//
//         using var request = new HttpRequestMessage(HttpMethod.Put, $"{Hostname}/mockserver/expectation")
//         {
//             Content = new StringContent(content, Encoding.UTF8, "application/json")
//         };
//         
//         using var response = await client.SendAsync(request);
//         
//         response.EnsureSuccessStatusCode();
//     }
//
//     public static async Task ResetAsync()
//     {
//         var client = _httpClient.Value!;
//         using var request = new HttpRequestMessage(HttpMethod.Put, $"{Hostname}/mockserver/reset");
//         using var response = await client.SendAsync(request);
//         response.EnsureSuccessStatusCode();
//     }
// }