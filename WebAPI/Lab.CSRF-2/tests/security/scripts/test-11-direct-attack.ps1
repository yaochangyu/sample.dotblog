# 測試案例 4.1: 直接攻擊 Protected API

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 4.1: 直接攻擊 Protected API" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }

Write-Host ""
Write-Host "攻擊場景: 使用 PowerShell 直接攻擊 Protected API" -ForegroundColor Yellow
Write-Host "--------------------------------------"

try {
    $body = @{ data = "直接攻擊測試" } | ConvertTo-Json
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
        Write-Host "✅ 測試通過: 直接攻擊被成功阻擋 (HTTP 403)" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "❌ 測試失敗: API 應該拒絕無 Token 的請求" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ 錯誤: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
