# CSRF 防護自動驗證腳本
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CSRF 防護能力自動驗證" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5073"
$testResults = @()

function Test-Scenario {
    param(
        [string]$TestName,
        [string]$Description,
        [scriptblock]$TestCode
    )
    
    Write-Host "測試: $TestName" -ForegroundColor Yellow
    Write-Host "說明: $Description" -ForegroundColor Gray
    
    try {
        $result = & $TestCode
        if ($result.Success) {
            Write-Host "✅ 通過" -ForegroundColor Green
        } else {
            Write-Host "❌ 失敗: $($result.Message)" -ForegroundColor Red
        }
        Write-Host ""
        
        $script:testResults += [PSCustomObject]@{
            Test = $TestName
            Status = if ($result.Success) { "PASS" } else { "FAIL" }
            Message = $result.Message
        }
        
        return $result.Success
    }
    catch {
        Write-Host "❌ 錯誤: $_" -ForegroundColor Red
        Write-Host ""
        
        $script:testResults += [PSCustomObject]@{
            Test = $TestName
            Status = "ERROR"
            Message = $_.Exception.Message
        }
        
        return $false
    }
}

# 測試 1: 正常流程
Test-Scenario -TestName "測試 1: 正常流程" -Description "驗證合法請求能通過" -TestCode {
    $tokenResponse = Invoke-WebRequest -Uri "$baseUrl/api/token?maxUsage=1&expirationMinutes=5" -Method Get
    $tokenData = $tokenResponse.Content | ConvertFrom-Json
    $token = $tokenData.token
    
    if (-not $token) {
        return @{ Success = $false; Message = "無法取得 Token" }
    }
    
    $headers = @{
        "X-CSRF-Token" = $token
        "Content-Type" = "application/json"
    }
    $body = @{ data = "測試資料" } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $body
    
    if ($response.StatusCode -eq 200) {
        return @{ Success = $true; Message = "正常請求成功 (HTTP $($response.StatusCode))" }
    } else {
        return @{ Success = $false; Message = "預期 200 但收到 $($response.StatusCode)" }
    }
}

# 測試 2: 缺少 Token
Test-Scenario -TestName "測試 2: 缺少 Token" -Description "驗證無 Token 請求被拒絕" -TestCode {
    try {
        $headers = @{
            "Content-Type" = "application/json"
        }
        $body = @{ data = "測試資料" } | ConvertTo-Json
        
        $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $body -ErrorAction Stop
        return @{ Success = $false; Message = "應該被拒絕但收到 HTTP $($response.StatusCode)" }
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 401 -or $_.Exception.Response.StatusCode.value__ -eq 403) {
            return @{ Success = $true; Message = "正確拒絕無 Token 請求 (HTTP $($_.Exception.Response.StatusCode.value__))" }
        } else {
            return @{ Success = $false; Message = "預期 401/403 但收到 $($_.Exception.Response.StatusCode.value__)" }
        }
    }
}

# 測試 3: 無效 Token
Test-Scenario -TestName "測試 3: 無效 Token" -Description "驗證偽造 Token 被拒絕" -TestCode {
    try {
        $fakeToken = "fake-token-12345-67890"
        $headers = @{
            "X-CSRF-Token" = $fakeToken
            "Content-Type" = "application/json"
        }
        $body = @{ data = "測試資料" } | ConvertTo-Json
        
        $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $body -ErrorAction Stop
        return @{ Success = $false; Message = "應該被拒絕但收到 HTTP $($response.StatusCode)" }
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 401) {
            return @{ Success = $true; Message = "正確拒絕無效 Token (HTTP 401)" }
        } else {
            return @{ Success = $false; Message = "預期 401 但收到 $($_.Exception.Response.StatusCode.value__)" }
        }
    }
}

# 測試 4: Token 重複使用
Test-Scenario -TestName "測試 4: Token 重複使用" -Description "驗證使用次數限制" -TestCode {
    $tokenResponse = Invoke-WebRequest -Uri "$baseUrl/api/token?maxUsage=1&expirationMinutes=5" -Method Get
    $tokenData = $tokenResponse.Content | ConvertFrom-Json
    $token = $tokenData.token
    
    $headers = @{
        "X-CSRF-Token" = $token
        "Content-Type" = "application/json"
    }
    $body = @{ data = "第一次呼叫" } | ConvertTo-Json
    
    # 第一次呼叫
    $response1 = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $body
    
    # 第二次呼叫
    try {
        Start-Sleep -Milliseconds 200
        $body = @{ data = "第二次呼叫" } | ConvertTo-Json
        $response2 = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $body -ErrorAction Stop
        return @{ Success = $false; Message = "第二次應該失敗但成功了 (HTTP $($response2.StatusCode))" }
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 401) {
            return @{ Success = $true; Message = "正確限制使用次數 (第一次:200, 第二次:401)" }
        } else {
            return @{ Success = $false; Message = "第二次預期 401 但收到 $($_.Exception.Response.StatusCode.value__)" }
        }
    }
}

# 測試 5: Token 過期
Test-Scenario -TestName "測試 5: Token 過期" -Description "驗證過期 Token 被拒絕" -TestCode {
    # 使用 1 分鐘過期，等待 70 秒確保過期
    $tokenResponse = Invoke-WebRequest -Uri "$baseUrl/api/token?maxUsage=1&expirationMinutes=1" -Method Get
    $tokenData = $tokenResponse.Content | ConvertFrom-Json
    $token = $tokenData.token
    
    Write-Host "  等待 Token 過期 (70 秒)..." -ForegroundColor Gray
    Start-Sleep -Seconds 70
    
    try {
        $headers = @{
            "X-CSRF-Token" = $token
            "Content-Type" = "application/json"
        }
        $body = @{ data = "使用過期 Token" } | ConvertTo-Json
        
        $response = Invoke-WebRequest -Uri "$baseUrl/api/protected" -Method Post -Headers $headers -Body $body -ErrorAction Stop
        return @{ Success = $false; Message = "過期 Token 應該被拒絕但收到 HTTP $($response.StatusCode)" }
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 401) {
            return @{ Success = $true; Message = "正確拒絕過期 Token (HTTP 401)" }
        } else {
            return @{ Success = $false; Message = "預期 401 但收到 $($_.Exception.Response.StatusCode.value__)" }
        }
    }
}

# 測試 6: 並發請求
Test-Scenario -TestName "測試 6: 並發請求" -Description "驗證並發請求處理" -TestCode {
    $tokenResponse = Invoke-WebRequest -Uri "$baseUrl/api/token?maxUsage=3&expirationMinutes=5" -Method Get
    $tokenData = $tokenResponse.Content | ConvertFrom-Json
    $token = $tokenData.token
    
    $headers = @{
        "X-CSRF-Token" = $token
        "Content-Type" = "application/json"
    }
    
    $jobs = @()
    for ($i = 1; $i -le 5; $i++) {
        $jobs += Start-Job -ScriptBlock {
            param($url, $headers, $index)
            try {
                $body = @{ data = "並發請求 $index" } | ConvertTo-Json
                $response = Invoke-WebRequest -Uri $url -Method Post -Headers $headers -Body $body -ErrorAction Stop
                return @{ Index = $index; StatusCode = $response.StatusCode; Success = $true }
            }
            catch {
                return @{ Index = $index; StatusCode = $_.Exception.Response.StatusCode.value__; Success = $false }
            }
        } -ArgumentList "$baseUrl/api/protected", $headers, $i
    }
    
    $results = $jobs | Wait-Job | Receive-Job
    $jobs | Remove-Job
    
    $successCount = ($results | Where-Object { $_.Success }).Count
    $failCount = ($results | Where-Object { -not $_.Success }).Count
    
    if ($successCount -eq 3 -and $failCount -eq 2) {
        return @{ Success = $true; Message = "並發請求正確處理 (成功:$successCount, 失敗:$failCount)" }
    } else {
        return @{ Success = $false; Message = "預期 成功:3 失敗:2, 但得到 成功:$successCount 失敗:$failCount" }
    }
}

# 顯示測試摘要
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  測試摘要" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$testResults | Format-Table -AutoSize

$passCount = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$errorCount = ($testResults | Where-Object { $_.Status -eq "ERROR" }).Count
$totalCount = $testResults.Count

Write-Host "總測試數: $totalCount" -ForegroundColor White
Write-Host "通過: $passCount" -ForegroundColor Green
Write-Host "失敗: $failCount" -ForegroundColor Red
Write-Host "錯誤: $errorCount" -ForegroundColor Yellow
Write-Host ""

if ($failCount -eq 0 -and $errorCount -eq 0) {
    Write-Host "🎉 所有測試通過！CSRF 防護機制運作正常。" -ForegroundColor Green
} else {
    Write-Host "⚠️ 存在失敗或錯誤的測試項目，請檢查。" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "測試完成時間: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
