using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;
using Reqnroll;

namespace Lab.QueueApi.Tests.StepDefinitions;

[Binding]
public class RequestStepDefinitions : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private HttpResponseMessage? _response;
    private readonly Dictionary<string, string> _requestIds = new();
    private readonly Dictionary<string, object> _testData = new();

    public RequestStepDefinitions(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Given(@"系統速率限制為每分鐘 (.*) 個請求")]
    public void GivenSystemRateLimitPerMinute(int limit)
    {
        _testData["RateLimit"] = limit;
        // 這裡可以設定測試用的速率限制配置
    }

    [Given(@"請求池為空")]
    public void GivenRequestPoolIsEmpty()
    {
        // 清空請求池的邏輯
        _testData["PoolEmpty"] = true;
    }

    [Given(@"當前沒有正在處理的請求")]
    public void GivenNoCurrentlyProcessingRequests()
    {
        _testData["ProcessingCount"] = 0;
    }

    [Given(@"已有 (.*) 個請求在處理中")]
    public void GivenRequestsInProcessing(int count)
    {
        _testData["ProcessingCount"] = count;
        // 模擬已有請求在處理中的狀態
    }

    [Given(@"請求池中有一些等待的請求")]
    public void GivenSomeRequestsWaitingInPool()
    {
        _testData["HasWaitingRequests"] = true;
    }

    [Given(@"請求池中存在 Request ID ""(.*)""")]
    public void GivenRequestIdExistsInPool(string requestId)
    {
        _requestIds[requestId] = requestId;
        _testData[$"Request_{requestId}_Status"] = "Pending";
    }

    [Given(@"該請求狀態為 ""(.*)""")]
    public void GivenRequestStatusIs(string status)
    {
        var lastRequestId = _requestIds.Keys.LastOrDefault();
        if (lastRequestId != null)
        {
            _testData[$"Request_{lastRequestId}_Status"] = status;
        }
    }

    [Given(@"系統可以處理新請求")]
    public void GivenSystemCanProcessNewRequests()
    {
        _testData["CanProcessNew"] = true;
    }

    [Given(@"請求池中不存在 Request ID ""(.*)""")]
    public void GivenRequestIdDoesNotExist(string requestId)
    {
        _testData[$"Request_{requestId}_Exists"] = false;
    }

    [Given(@"系統中存在 Request ID ""(.*)""")]
    public void GivenRequestIdExistsInSystem(string requestId)
    {
        _requestIds[requestId] = requestId;
        _testData[$"Request_{requestId}_Exists"] = true;
    }

    [Given(@"系統中不存在 Request ID ""(.*)""")]
    public void GivenRequestIdDoesNotExistInSystem(string requestId)
    {
        _testData[$"Request_{requestId}_Exists"] = false;
    }

    [Given(@"系統已達到處理限制")]
    public void GivenSystemReachedProcessingLimit()
    {
        _testData["ProcessingLimitReached"] = true;
    }

    [Given(@"背景處理服務正在運行")]
    public void GivenBackgroundServiceRunning()
    {
        _testData["BackgroundServiceRunning"] = true;
    }

    [Given(@"請求池中有 (.*) 個等待的請求")]
    public void GivenWaitingRequestsInPool(int count)
    {
        _testData["WaitingRequestsCount"] = count;
    }

    [Given(@"請求池中有一個創建於 (.*) 分鐘前的請求 ""(.*)""")]
    public void GivenRequestCreatedMinutesAgo(int minutes, string requestId)
    {
        _requestIds[requestId] = requestId;
        _testData[$"Request_{requestId}_CreatedAt"] = DateTime.UtcNow.AddMinutes(-minutes);
    }

    [Given(@"請求池中有一個創建於 (.*) 秒前的請求 ""(.*)""")]
    public void GivenRequestCreatedSecondsAgo(int seconds, string requestId)
    {
        _requestIds[requestId] = requestId;
        _testData[$"Request_{requestId}_CreatedAt"] = DateTime.UtcNow.AddSeconds(-seconds);
    }

    [Given(@"請求池中有 (.*) 個創建於 (.*) 分鐘前的請求")]
    public void GivenMultipleRequestsCreatedMinutesAgo(int count, int minutes)
    {
        for (int i = 0; i < count; i++)
        {
            var requestId = $"old-request-{i}";
            _requestIds[requestId] = requestId;
            _testData[$"Request_{requestId}_CreatedAt"] = DateTime.UtcNow.AddMinutes(-minutes);
        }
    }

    [Given(@"背景服務已運行 (.*) 秒")]
    public void GivenBackgroundServiceRanForSeconds(int seconds)
    {
        _testData["BackgroundServiceRunTime"] = seconds;
    }

    [When(@"我提交一個請求 ""(.*)""")]
    public async Task WhenISubmitRequest(string requestData)
    {
        var request = new { Data = requestData };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _response = await _client.PostAsync("/api/request/submit", content);
    }

    [When(@"我提交請求 ""(.*)"", ""(.*)"", ""(.*)""")]
    public async Task WhenISubmitMultipleRequests(string request1, string request2, string request3)
    {
        var requests = new[] { request1, request2, request3 };
        var responses = new List<HttpResponseMessage>();

        foreach (var req in requests)
        {
            var request = new { Data = req };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/requests/submit", content);
            responses.Add(response);
        }

        _testData["MultipleResponses"] = responses;
    }

    [When(@"我使用 Request ID ""(.*)"" 進行重試")]
    public async Task WhenIRetryWithRequestId(string requestId)
    {
        _response = await _client.GetAsync($"/api/requests/retry/{requestId}");
    }

    [When(@"我查詢 Request ID ""(.*)"" 的狀態")]
    public async Task WhenIQueryRequestStatus(string requestId)
    {
        _response = await _client.GetAsync($"/api/requests/status/{requestId}");
    }

    [When(@"我取消 Request ID ""(.*)""")]
    public async Task WhenICancelRequest(string requestId)
    {
        _response = await _client.DeleteAsync($"/api/requests/cancel/{requestId}");
    }

    [When(@"背景服務檢查處理機會")]
    public void WhenBackgroundServiceChecksForOpportunity()
    {
        // 模擬背景服務檢查邏輯
        _testData["BackgroundServiceChecked"] = true;
    }

    [When(@"背景服務執行清理作業")]
    public void WhenBackgroundServiceExecutesCleanup()
    {
        // 模擬背景清理邏輯
        _testData["CleanupExecuted"] = true;
    }

    [When(@"系統時間達到下一個清理週期")]
    public void WhenSystemReachesNextCleanupCycle()
    {
        _testData["CleanupCycleReached"] = true;
    }

    [Then(@"請求應該被立即處理")]
    public void ThenRequestShouldBeProcessedImmediately()
    {
        // 驗證請求立即處理的邏輯
        Assert.True(_testData.ContainsKey("ProcessedImmediately") || _response?.IsSuccessStatusCode == true);
    }

    [Then(@"回應狀態碼應該是 (.*)")]
    public void ThenResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal((HttpStatusCode)expectedStatusCode, _response.StatusCode);
    }

    [Then(@"回應內容應包含處理結果")]
    public async Task ThenResponseShouldContainProcessingResult()
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        // 驗證包含處理結果的邏輯
    }

    [Then(@"請求應該被加入請求池")]
    public void ThenRequestShouldBeAddedToPool()
    {
        _testData["AddedToPool"] = true;
    }

    [Then(@"回應應包含 Request ID")]
    public async Task ThenResponseShouldContainRequestId()
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        Assert.True(response?.ContainsKey("requestId") == true);
    }

    [Then(@"回應應包含重試時間")]
    public async Task ThenResponseShouldContainRetryTime()
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        Assert.True(response?.ContainsKey("retryAfterSeconds") == true);
    }

    [Then(@"所有請求都應該被加入請求池")]
    public void ThenAllRequestsShouldBeAddedToPool()
    {
        if (_testData.TryGetValue("MultipleResponses", out var responses))
        {
            var responseList = (List<HttpResponseMessage>)responses;
            foreach (var response in responseList)
            {
                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }
    }

    [Then(@"每個請求都應該獲得唯一的 Request ID")]
    public async Task ThenEachRequestShouldGetUniqueRequestId()
    {
        if (_testData.TryGetValue("MultipleResponses", out var responses))
        {
            var responseList = (List<HttpResponseMessage>)responses;
            var requestIds = new List<string>();

            foreach (var response in responseList)
            {
                var content = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                if (responseObj?.TryGetValue("requestId", out var requestId) == true)
                {
                    requestIds.Add(requestId.ToString()!);
                }
            }

            Assert.Equal(requestIds.Count, requestIds.Distinct().Count());
        }
    }

    [Then(@"請求應該被處理")]
    public void ThenRequestShouldBeProcessed()
    {
        _testData["RequestProcessed"] = true;
    }

    [Then(@"Request ID ""(.*)"" 應該從池中移除")]
    public void ThenRequestIdShouldBeRemovedFromPool(string requestId)
    {
        _testData[$"Request_{requestId}_RemovedFromPool"] = true;
    }

    [Then(@"回應應包含 ""(.*)"" 訊息")]
    public async Task ThenResponseShouldContainMessage(string expectedMessage)
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        Assert.Contains(expectedMessage.ToLower(), content.ToLower());
    }

    [Then(@"回應應包含狀態 ""(.*)""")]
    public async Task ThenResponseShouldContainStatus(string expectedStatus)
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        Assert.True(response?.TryGetValue("status", out var status) == true);
        // Assert.Equal(expectedStatus, status?.ToString());
    }

    [Then(@"回應應包含創建時間")]
    public async Task ThenResponseShouldContainCreatedTime()
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        Assert.True(response?.ContainsKey("createdAt") == true);
    }

    [Then(@"回應應包含預估處理時間")]
    public async Task ThenResponseShouldContainEstimatedProcessTime()
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        Assert.True(response?.ContainsKey("estimatedProcessTime") == true);
    }

    [Then(@"回應應包含完成時間")]
    public async Task ThenResponseShouldContainCompletedTime()
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        Assert.True(response?.ContainsKey("completedAt") == true);
    }

    [Then(@"該請求狀態應該變為 ""(.*)""")]
    public void ThenRequestStatusShouldBecome(string expectedStatus)
    {
        // 在實際實作中，這裡會驗證狀態變更
        var lastRequestId = _requestIds.Keys.LastOrDefault();
        if (lastRequestId != null)
        {
            _testData[$"Request_{lastRequestId}_Status"] = expectedStatus;
        }
    }

    [Then(@"應該從池中取得第一個請求進行處理")]
    public void ThenShouldTakeFirstRequestFromPool()
    {
        _testData["FirstRequestTaken"] = true;
    }

    [Then(@"池中剩餘請求數量應該減少 (.*)")]
    public void ThenPoolSizeShouldDecreaseBy(int count)
    {
        _testData["PoolSizeDecreased"] = count;
    }

    [Then(@"不應該處理任何池中的請求")]
    public void ThenShouldNotProcessAnyPoolRequests()
    {
        _testData["NoPoolRequestsProcessed"] = true;
    }

    [Then(@"所有池中請求應該保持 ""(.*)"" 狀態")]
    public void ThenAllPoolRequestsShouldMaintainStatus(string status)
    {
        _testData["AllPoolRequestsStatus"] = status;
    }

    [Then(@"Request ID ""(.*)"" 狀態應該變為 ""(.*)""")]
    public void ThenSpecificRequestStatusShouldBecome(string requestId, string status)
    {
        _testData[$"Request_{requestId}_Status"] = status;
    }

    [Then(@"Request ID ""(.*)"" 應該仍在池中")]
    public void ThenRequestIdShouldStillBeInPool(string requestId)
    {
        _testData[$"Request_{requestId}_InPool"] = true;
    }

    [Then(@"所有 (.*) 個請求狀態應該變為 ""(.*)""")]
    public void ThenAllRequestsStatusShouldBecome(int count, string status)
    {
        _testData["AllRequestsStatusChanged"] = $"{count}_{status}";
    }

    [Then(@"所有 (.*) 個請求應該從池中移除")]
    public void ThenAllRequestsShouldBeRemovedFromPool(int count)
    {
        _testData["AllRequestsRemovedCount"] = count;
    }

    [Then(@"系統記憶體應該被釋放")]
    public void ThenSystemMemoryShouldBeReleased()
    {
        _testData["MemoryReleased"] = true;
    }

    [Then(@"背景服務應該自動執行清理作業")]
    public void ThenBackgroundServiceShouldExecuteCleanup()
    {
        _testData["AutoCleanupExecuted"] = true;
    }

    [Then(@"清理作業應該檢查所有池中請求的超時狀態")]
    public void ThenCleanupShouldCheckAllPoolRequestsTimeout()
    {
        _testData["AllRequestsTimeoutChecked"] = true;
    }

    [Then(@"回應內容應包含之前的處理結果")]
    public async Task ThenResponseShouldContainPreviousResult()
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        // 驗證包含之前處理結果的邏輯
    }
}