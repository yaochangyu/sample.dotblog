using Newtonsoft.Json;

namespace Lab.LineBot.SDK.Models
{
    public class TokenResponse : GenericResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}