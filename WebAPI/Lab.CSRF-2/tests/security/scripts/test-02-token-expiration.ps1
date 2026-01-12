# 測試案例 1.2: Token 過期測試

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 1.2: Token 過期測試" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }

Write-Host ""
Write-Host "步驟 1: 取得 Token (設定 1 分鐘過期)" -ForegroundColor Yellow
Write-Host "--------------------------------------"

try {
    $response = Invoke-WebRequest -Uri "$API_BASE/api/token?maxUsage=5&expirationMinutes=1" -Method Get
    $token = $response.Headers["X-CSRF-Token"]
    
    if (-not $token) {
        Write-Host "❌ 失敗: 無法取得 Token" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Token 取得成功: $token" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "步驟 2: 等待 61 秒讓 Token 過期..." -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    
    for ($i = 61; $i -gt 0; $i--) {
        Write-Host "`r剩餘時間: $i 秒 " -NoNewline
        Start-Sleep -Seconds 1
    }
    Write-Host ""
    
    Write-Host ""
    Write-Host "步驟 3: 使用過期 Token 呼叫 API" -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    
    $body = @{ data = "測試過期 Token" } | ConvertTo-Json
    $headers = @{
        "Content-Type" = "application/json"
        "X-CSRF-Token" = $token
    }
    
    try {
        $apiResponse = Invoke-WebRequest -Uri "$API_BASE/api/protected" -Method Post -Headers $headers -Body $body
        $statusCode = $apiResponse.StatusCode
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        $body = $_.ErrorDetails.Message
    }
    
    Write-Host "HTTP 狀態碼: $statusCode" -ForegroundColor Cyan
    Write-Host "回應內容: $body" -ForegroundColor Cyan
    
    if ($statusCode -eq 403) {
        Write-Host ""
        Write-Host "✅ 測試通過: 過期 Token 被正確拒絕 (HTTP 403)" -ForegroundColor Green
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
