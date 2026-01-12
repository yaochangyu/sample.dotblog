# 測試案例 2.3: User-Agent 不一致

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 2.3: User-Agent 不一致" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }
$USER_AGENT_A = "Mozilla/5.0 (TestClient-A)"
$USER_AGENT_B = "Mozilla/5.0 (TestClient-B)"

Write-Host ""
Write-Host "步驟 1: 使用 User-Agent A 取得 Token" -ForegroundColor Yellow
Write-Host "--------------------------------------"
Write-Host "User-Agent: $USER_AGENT_A" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$API_BASE/api/token?maxUsage=5&expirationMinutes=5" -Method Get -UserAgent $USER_AGENT_A
    $token = $response.Headers["X-CSRF-Token"]
    
    if (-not $token) {
        Write-Host "❌ 失敗: 無法取得 Token" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Token 取得成功: $token" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "步驟 2: 使用 User-Agent B 呼叫 Protected API" -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    Write-Host "User-Agent: $USER_AGENT_B" -ForegroundColor Yellow
    
    $body = @{ data = "測試 User-Agent 不一致" } | ConvertTo-Json
    $headers = @{
        "Content-Type" = "application/json"
        "X-CSRF-Token" = $token
    }
    
    try {
        $apiResponse = Invoke-WebRequest -Uri "$API_BASE/api/protected" -Method Post -Headers $headers -Body $body -UserAgent $USER_AGENT_B
        $statusCode = $apiResponse.StatusCode
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        $responseBody = $_.ErrorDetails.Message
    }
    
    Write-Host "HTTP 狀態碼: $statusCode" -ForegroundColor Cyan
    Write-Host "回應內容: $responseBody" -ForegroundColor Cyan
    
    if ($statusCode -eq 403) {
        Write-Host ""
        Write-Host "✅ 測試通過: User-Agent 不一致的請求被正確拒絕 (HTTP 403)" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "❌ 測試失敗: 預期 HTTP 403，實際 HTTP $statusCode" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ 錯誤: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
