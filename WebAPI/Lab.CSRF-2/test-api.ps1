# WebAPI Token 防濫用機制測試腳本

Write-Host "=== WebAPI Token 防濫用機制測試 ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://localhost:7001"
$httpUrl = "http://localhost:5000"

# 測試 1: 取得 Token
Write-Host "測試 1: 取得 Token" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/token?maxUsage=2&expirationMinutes=5" -Method Get -SkipCertificateCheck
    $token = $response.Headers['X-CSRF-Token']
    $body = $response.Content | ConvertFrom-Json
    
    Write-Host "✓ Token 取得成功" -ForegroundColor Green
    Write-Host "Token: $token" -ForegroundColor White
    Write-Host "回應: $($body | ConvertTo-Json)" -ForegroundColor Gray
} catch {
    Write-Host "✗ 取得 Token 失敗: $_" -ForegroundColor Red
    exit
}

Write-Host ""

# 測試 2: 使用有效 Token 呼叫 API (第一次)
Write-Host "測試 2: 使用有效 Token 呼叫 Protected API (第一次)" -ForegroundColor Yellow
try {
    $headers = @{
        "X-CSRF-Token" = $token
        "Content-Type" = "application/json"
    }
    $requestBody = @{ data = "測試資料第一次" } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $requestBody -SkipCertificateCheck
    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "✓ API 呼叫成功 (狀態: $($response.StatusCode))" -ForegroundColor Green
    Write-Host "回應: $($result | ConvertTo-Json)" -ForegroundColor Gray
} catch {
    Write-Host "✗ API 呼叫失敗: $_" -ForegroundColor Red
}

Write-Host ""

# 測試 3: 使用相同 Token 再次呼叫 (第二次，應該成功因為 maxUsage=2)
Write-Host "測試 3: 使用相同 Token 呼叫 Protected API (第二次)" -ForegroundColor Yellow
try {
    $headers = @{
        "X-CSRF-Token" = $token
        "Content-Type" = "application/json"
    }
    $requestBody = @{ data = "測試資料第二次" } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $requestBody -SkipCertificateCheck
    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "✓ API 呼叫成功 (狀態: $($response.StatusCode))" -ForegroundColor Green
    Write-Host "回應: $($result | ConvertTo-Json)" -ForegroundColor Gray
} catch {
    Write-Host "✗ API 呼叫失敗: $_" -ForegroundColor Red
}

Write-Host ""

# 測試 4: 使用相同 Token 第三次呼叫 (應該失敗因為超過 maxUsage)
Write-Host "測試 4: 使用相同 Token 呼叫 Protected API (第三次，應該失敗)" -ForegroundColor Yellow
try {
    $headers = @{
        "X-CSRF-Token" = $token
        "Content-Type" = "application/json"
    }
    $requestBody = @{ data = "測試資料第三次" } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $requestBody -SkipCertificateCheck -ErrorAction Stop
    Write-Host "✗ 預期失敗但成功了！" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ 正確拒絕 (狀態: 401 Unauthorized)" -ForegroundColor Green
    } else {
        Write-Host "✗ 非預期錯誤: $_" -ForegroundColor Red
    }
}

Write-Host ""

# 測試 5: 使用無效 Token
Write-Host "測試 5: 使用無效 Token" -ForegroundColor Yellow
try {
    $headers = @{
        "X-CSRF-Token" = "invalid-token-12345"
        "Content-Type" = "application/json"
    }
    $requestBody = @{ data = "測試資料" } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $requestBody -SkipCertificateCheck -ErrorAction Stop
    Write-Host "✗ 預期失敗但成功了！" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ 正確拒絕無效 Token (狀態: 401 Unauthorized)" -ForegroundColor Green
    } else {
        Write-Host "✗ 非預期錯誤: $_" -ForegroundColor Red
    }
}

Write-Host ""

# 測試 6: 缺少 Token Header
Write-Host "測試 6: 缺少 Token Header" -ForegroundColor Yellow
try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    $requestBody = @{ data = "測試資料" } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $requestBody -SkipCertificateCheck -ErrorAction Stop
    Write-Host "✗ 預期失敗但成功了！" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ 正確拒絕缺少 Token 的請求 (狀態: 401 Unauthorized)" -ForegroundColor Green
    } else {
        Write-Host "✗ 非預期錯誤: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== 測試完成 ===" -ForegroundColor Cyan
