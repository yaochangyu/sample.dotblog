using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace Client.WinFormsNet48
{
    public class LabService
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