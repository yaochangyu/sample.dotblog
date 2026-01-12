# 測試案例 2.2: 無效 Token

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 2.2: 無效 Token" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }
$FAKE_TOKEN = "12345678-1234-1234-1234-123456789abc"

Write-Host ""
Write-Host "步驟: 使用偽造的 Token 呼叫 Protected API" -ForegroundColor Yellow
Write-Host "--------------------------------------"
Write-Host "偽造 Token: $FAKE_TOKEN" -ForegroundColor Yellow

try {
    $body = @{ data = "測試無效 Token" } | ConvertTo-Json
    $headers = @{
        "Content-Type" = "application/json"
        "X-CSRF-Token" = $FAKE_TOKEN
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$API_BASE/api/protected" -Method Post -Headers $headers -Body $body
        $statusCode = $response.StatusCode
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        $responseBody = $_.ErrorDetails.Message
    }
    
    Write-Host "HTTP 狀態碼: $statusCode" -ForegroundColor Cyan
    Write-Host "回應內容: $responseBody" -ForegroundColor Cyan
    
    if ($statusCode -eq 403) {
        Write-Host ""
        Write-Host "✅ 測試通過: 無效 Token 被正確拒絕 (HTTP 403)" -ForegroundColor Green
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
