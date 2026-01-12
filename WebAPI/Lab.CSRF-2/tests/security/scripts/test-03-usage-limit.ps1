# 測試案例 1.3: Token 使用次數限制

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 1.3: Token 使用次數限制" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }

Write-Host ""
Write-Host "步驟 1: 取得 Token (最大使用次數 = 1)" -ForegroundColor Yellow
Write-Host "--------------------------------------"

try {
    $response = Invoke-WebRequest -Uri "$API_BASE/api/token?maxUsage=1&expirationMinutes=5" -Method Get
    $token = $response.Headers["X-CSRF-Token"]
    
    if (-not $token) {
        Write-Host "❌ 失敗: 無法取得 Token" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Token 取得成功: $token" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "步驟 2: 第一次呼叫 Protected API (應該成功)" -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    
    $body = @{ data = "第一次呼叫" } | ConvertTo-Json
    $headers = @{
        "Content-Type" = "application/json"
        "X-CSRF-Token" = $token
    }
    
    $response1 = Invoke-WebRequest -Uri "$API_BASE/api/protected" -Method Post -Headers $headers -Body $body
    
    Write-Host "HTTP 狀態碼: $($response1.StatusCode)" -ForegroundColor Cyan
    Write-Host "回應內容: $($response1.Content)" -ForegroundColor Cyan
    
    if ($response1.StatusCode -ne 200) {
        Write-Host "❌ 第一次呼叫失敗" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ 第一次呼叫成功" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "步驟 3: 第二次呼叫 Protected API (應該失敗)" -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    
    try {
        $response2 = Invoke-WebRequest -Uri "$API_BASE/api/protected" -Method Post -Headers $headers -Body (@{ data = "第二次呼叫" } | ConvertTo-Json)
        $status2 = $response2.StatusCode
    }
    catch {
        $status2 = $_.Exception.Response.StatusCode.Value__
    }
    
    Write-Host "HTTP 狀態碼: $status2" -ForegroundColor Cyan
    
    if ($status2 -eq 403) {
        Write-Host ""
        Write-Host "✅ 測試通過: 使用次數限制正常運作" -ForegroundColor Green
        Write-Host "   - 第一次呼叫: HTTP $($response1.StatusCode) (成功)" -ForegroundColor Green
        Write-Host "   - 第二次呼叫: HTTP $status2 (被拒絕)" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "❌ 測試失敗: 第二次呼叫應該被拒絕，但得到 HTTP $status2" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ 錯誤: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
