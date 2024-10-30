using System.Text.Json.Serialization;

namespace Lab.Sharding.Testing.Common.MockServer.Contracts;

public class HttpRequest
{
    public string Method { get; set; }
    public string Path { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Cookies Cookies { get; set; }
}