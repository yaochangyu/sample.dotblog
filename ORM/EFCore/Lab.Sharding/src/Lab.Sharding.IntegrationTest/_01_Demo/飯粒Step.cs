using System.Text.Json.Nodes;
using Reqnroll;

namespace JobBank1111.Job.WebAPI.IntegrationTest._01_Demo;

[Binding]
public class 飯粒Step : Steps
{
    [Given(@"已存在 Json 內容")]
    public void Given已存在Json內容(string json)
    {
        var jsonNode = JsonNode.Parse(json);
        this.ScenarioContext.SetJsonNode(jsonNode);
    }

    [When(@"模擬呼叫 API，得到以下內容")]
    public void When模擬呼叫api得到以下內容(string json)
    {
        this.ScenarioContext.SetHttpResponseBody(json);
    }
}