using Newtonsoft.Json;

namespace Lab.LineBot.SDK.Models
{
    public class GenericResponse
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}