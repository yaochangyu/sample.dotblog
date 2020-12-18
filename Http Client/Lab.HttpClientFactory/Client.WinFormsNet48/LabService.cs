using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace Client.WinFormsNet48
{
    public interface ILabService
    {
        IEnumerable<string> Get();
    }

    public class LabService2 : ILabService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LabService2(IHttpClientFactory factory)
        {
            this._httpClientFactory = factory;
        }

        public IEnumerable<string> Get()
        {
            var url      = "api/default";
            var client   = this._httpClientFactory.CreateClient("lab");
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result  = JsonConvert.DeserializeObject<string[]>(content);

            return result;
        }
    }

    public class LabService : ILabService
    {
        private readonly HttpClient _client;

        public LabService(HttpClient client)
        {
            this._client = client;
        }

        public IEnumerable<string> Get()
        {
            var url      = "api/default";
            var response = this._client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result  = JsonConvert.DeserializeObject<string[]>(content);

            return result;
        }
    }
}