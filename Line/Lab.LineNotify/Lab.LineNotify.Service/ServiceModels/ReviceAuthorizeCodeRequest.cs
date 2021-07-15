using Newtonsoft.Json;

namespace Lab.LineNotify.Service.ServiceModels
{
    public class ReceiveAuthorizeCodeRequest
    {
        
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }
    }
}