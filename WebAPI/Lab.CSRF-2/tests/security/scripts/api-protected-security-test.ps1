# API Protected 安全測試腳本
# 執行 10 項核心安全測試並產生報告

param(
    [string]$BaseUrl = "http://localhost:5073"
)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "API Protected 安全測試" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
Write-Host "測試目標: $BaseUrl`n" -ForegroundColor White

$results = @()

# 測試函式
function Test-API {
    param(
        [string]$TestId,
        [string]$Name,
        [scriptblock]$Test,
        [string]$Expected
    )
    
    Write-Host "執行 $TestId - $Name..." -ForegroundColor Yellow
    $result = & $Test
    $passed = $result.Passed
    
    $script:results += [PSCustomObject]@{
        TestId = $TestId
        Name = $Name
        Expected = $Expected
        Actual = $result.Actual
        Status = if ($passed) { "✅ PASS" } else { "❌ FAIL" }
        Message = $result.Message
    }
    
    if ($passed) {
        Write-Host "  ✅ PASS - $($result.Message)" -ForegroundColor Green
    } else {
        Write-Host "  ❌ FAIL - $($result.Message)" -ForegroundColor Red
    }
    
    return $passed
}

# 安全請求函式
function Invoke-SafeRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null
    )
    
    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            Headers = $Headers
            SkipCertificateCheck = $true
            TimeoutSec = 5
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-WebRequest @params
        return @{ Status = $response.StatusCode; Success = $true }
    }
    catch {
        return @{ Status = $_.Exception.Response.StatusCode.value__; Success = $false }
    }
}

# 取得 Token
function Get-Token {
    param([hashtable]$Headers = @{})
    $response = Invoke-SafeRequest -Uri "$BaseUrl/api/token" -Headers $Headers
    if ($response.Success) {
        $tokenResponse = Invoke-WebRequest -Uri "$BaseUrl/api/token" -Headers $Headers -SkipCertificateCheck
        return $tokenResponse.Headers["X-CSRF-Token"][0]
    }
    return $null
}

$script:results = @()
$passCount = 0
$failCount = 0

# TC-CSRF-01
Test-API -TestId "TC-CSRF-01" -Name "無 Token 的請求" -Expected "401" -Test {
    $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Body '{"data":"test"}'
    $passed = $r.Status -eq 401
    return @{
        Passed = $passed
        Actual = $r.Status
        Message = if ($passed) { "正確拒絕無 Token 請求" } else { "未正確拒絕" }
    }
}

# TC-CSRF-02
Test-API -TestId "TC-CSRF-02" -Name "偽造 Token 的請求" -Expected "401" -Test {
    $fakeToken = [Guid]::NewGuid().ToString()
    $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$fakeToken} -Body '{"data":"test"}'
    $passed = $r.Status -eq 401
    return @{
        Passed = $passed
        Actual = $r.Status
        Message = if ($passed) { "正確拒絕假 Token" } else { "未正確拒絕假 Token" }
    }
}

# TC-CSRF-03
Test-API -TestId "TC-CSRF-03" -Name "過期 Token 的請求" -Expected "401" -Test {
    $token = Get-Token
    Start-Sleep -Seconds 6
    $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$token} -Body '{"data":"test"}'
    $passed = $r.Status -eq 401
    return @{
        Passed = $passed
        Actual = $r.Status
        Message = if ($passed) { "Token 正確過期" } else { "Token 未過期 (TTL 設定過長)" }
    }
}

# TC-CSRF-04
Test-API -TestId "TC-CSRF-04" -Name "重複使用 Token" -Expected "第1次:200, 第2次:401" -Test {
    $token = Get-Token
    $r1 = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$token} -Body '{"data":"test"}'
    $r2 = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$token} -Body '{"data":"test"}'
    $passed = ($r1.Status -eq 200) -and ($r2.Status -eq 401)
    return @{
        Passed = $passed
        Actual = "第1次:$($r1.Status), 第2次:$($r2.Status)"
        Message = if ($passed) { "次數限制正常" } else { "次數限制異常" }
    }
}

# TC-CSRF-05
Test-API -TestId "TC-CSRF-05" -Name "CORS 政策檢查" -Expected "不允許 *" -Test {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/token" -SkipCertificateCheck
    $cors = $response.Headers["Access-Control-Allow-Origin"]
    $passed = -not ($cors -and $cors[0] -eq "*")
    return @{
        Passed = $passed
        Actual = if ($cors) { $cors[0] } else { "無 CORS Header" }
        Message = if ($passed) { "CORS 設定安全" } else { "⚠️ AllowAnyOrigin 有風險" }
    }
}

# TC-LEAK-01
Test-API -TestId "TC-LEAK-01" -Name "cURL 使用洩漏 Token" -Expected "403" -Test {
    $token = Get-Token -Headers @{"User-Agent"="Mozilla/5.0"}
    $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$token;"User-Agent"="curl/7.68.0"} -Body '{"data":"test"}'
    $passed = $r.Status -eq 403
    return @{
        Passed = $passed
        Actual = $r.Status
        Message = if ($passed) { "User-Agent 驗證正常" } else { "⚠️ 無 User-Agent 檢查" }
    }
}

# TC-LEAK-02
Test-API -TestId "TC-LEAK-02" -Name "Token 批次請求" -Expected "僅1次成功" -Test {
    $token = Get-Token
    $successCount = 0
    for ($i = 1; $i -le 5; $i++) {
        $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$token} -Body '{"data":"test"}'
        if ($r.Status -eq 200) { $successCount++ }
    }
    $passed = $successCount -eq 1
    return @{
        Passed = $passed
        Actual = "$successCount 次成功"
        Message = if ($passed) { "次數限制生效" } else { "次數限制異常" }
    }
}

# TC-BOT-01
Test-API -TestId "TC-BOT-01" -Name "無 User-Agent 請求" -Expected "403" -Test {
    $token = Get-Token
    $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$token;"User-Agent"=""} -Body '{"data":"test"}'
    $passed = $r.Status -eq 403
    return @{
        Passed = $passed
        Actual = $r.Status
        Message = if ($passed) { "拒絕空 User-Agent" } else { "⚠️ 未驗證 User-Agent" }
    }
}

# TC-BOT-02
Test-API -TestId "TC-BOT-02" -Name "爬蟲 User-Agent" -Expected "403" -Test {
    $token = Get-Token
    $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"X-CSRF-Token"=$token;"User-Agent"="python-requests/2.28.0"} -Body '{"data":"test"}'
    $passed = $r.Status -eq 403
    return @{
        Passed = $passed
        Actual = $r.Status
        Message = if ($passed) { "阻擋爬蟲" } else { "⚠️ 無爬蟲黑名單" }
    }
}

# TC-CURL-01
Test-API -TestId "TC-CURL-01" -Name "cURL 無 Token" -Expected "401" -Test {
    $r = Invoke-SafeRequest -Uri "$BaseUrl/api/protected" -Method POST -Headers @{"User-Agent"="curl/7.68.0"} -Body '{"data":"test"}'
    $passed = $r.Status -eq 401
    return @{
        Passed = $passed
        Actual = $r.Status
        Message = if ($passed) { "正確拒絕" } else { "未拒絕" }
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "測試完成" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$totalTests = $script:results.Count
$passCount = ($script:results | Where-Object { $_.Status -like "*PASS*" }).Count
$failCount = $totalTests - $passCount
$passRate = if ($totalTests -gt 0) { [math]::Round(($passCount / $totalTests) * 100, 2) } else { 0 }

Write-Host "總測試: $totalTests" -ForegroundColor White
Write-Host "✅ 通過: $passCount" -ForegroundColor Green
Write-Host "❌ 失敗: $failCount" -ForegroundColor Red
Write-Host "通過率: $passRate%`n" -ForegroundColor $(if ($passRate -ge 80) { "Green" } elseif ($passRate -ge 60) { "Yellow" } else { "Red" })

# 顯示詳細結果
Write-Host "`n測試明細:`n" -ForegroundColor Cyan
$script:results | Format-Table -AutoSize TestId, Name, Status, Expected, Actual, Message

Write-Host "`n✅ 測試完成！" -ForegroundColor Green
