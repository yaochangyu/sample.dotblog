# 測試案例 1.1: 正常 Token 取得與使用

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 1.1: 正常 Token 取得與使用" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }
$USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"

Write-Host ""
Write-Host "步驟 1: 取得 Token" -ForegroundColor Yellow
Write-Host "--------------------------------------"

try {
    $headers = @{
        "User-Agent" = $USER_AGENT
        "Referer" = "$API_BASE/"
    }
    $response = Invoke-WebRequest -Uri "$API_BASE/api/token?maxUsage=1&expirationMinutes=5" -Method Get -Headers $headers
    $token = $response.Headers["X-CSRF-Token"]
    
    if (-not $token) {
        Write-Host "❌ 失敗: 無法取得 Token" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Token 取得成功: $token" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "步驟 2: 使用 Token 呼叫 Protected API" -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    
    $body = @{ data = "測試資料" } | ConvertTo-Json
    $headers = @{
        "Content-Type" = "application/json"
        "User-Agent" = $USER_AGENT
        "Referer" = "$API_BASE/"
        "X-CSRF-Token" = $token
    }
    
    $apiResponse = Invoke-WebRequest -Uri "$API_BASE/api/protected" -Method Post -Headers $headers -Body $body
    
    Write-Host "HTTP 狀態碼: $($apiResponse.StatusCode)" -ForegroundColor Cyan
    Write-Host "回應內容: $($apiResponse.Content)" -ForegroundColor Cyan
    
    if ($apiResponse.StatusCode -eq 200) {
        Write-Host ""
        Write-Host "✅ 測試通過: Protected API 呼叫成功" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "❌ 測試失敗: 預期 HTTP 200，實際 HTTP $($apiResponse.StatusCode)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ 錯誤: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
