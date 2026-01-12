# 測試案例 2.1: 無 Token 請求

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 2.1: 無 Token 請求" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }

Write-Host ""
Write-Host "步驟: 直接呼叫 Protected API 不帶 Token" -ForegroundColor Yellow
Write-Host "--------------------------------------"

try {
    $body = @{ data = "測試無 Token 請求" } | ConvertTo-Json
    $headers = @{ "Content-Type" = "application/json" }
    
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
        Write-Host "✅ 測試通過: 無 Token 請求被正確拒絕 (HTTP 403)" -ForegroundColor Green
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
