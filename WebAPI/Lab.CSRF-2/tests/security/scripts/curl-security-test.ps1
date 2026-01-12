# API 安全性測試腳本 - 使用 cURL
# 測試 api/protected 端點的 Token 驗證機制

Write-Host "=== API 安全性測試 (cURL 版本) ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5073"
$testsPassed = 0
$testsFailed = 0

function Test-ApiEndpoint {
    param(
        [string]$TestName,
        [string]$Url,
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [int]$ExpectedStatusCode,
        [string]$Description
    )
    
    Write-Host "測試: $TestName" -ForegroundColor Yellow
    Write-Host "說明: $Description" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Url
            Method = 'POST'
            Headers = $Headers
            ErrorAction = 'SilentlyContinue'
        }
        
        if ($Body) {
            $params['Body'] = $Body
            $params['ContentType'] = 'application/json'
        }
        
        $response = Invoke-WebRequest @params
        $statusCode = $response.StatusCode
        
        if ($statusCode -eq $ExpectedStatusCode) {
            Write-Host "✓ 測試通過 (狀態: $statusCode)" -ForegroundColor Green
            $script:testsPassed++
        } else {
            Write-Host "✗ 測試失敗 (預期: $ExpectedStatusCode, 實際: $statusCode)" -ForegroundColor Red
            $script:testsFailed++
        }
    } catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        if ($statusCode -eq $ExpectedStatusCode) {
            Write-Host "✓ 測試通過 (狀態: $statusCode)" -ForegroundColor Green
            $script:testsPassed++
        } else {
            Write-Host "✗ 測試失敗 (預期: $ExpectedStatusCode, 實際: $statusCode)" -ForegroundColor Red
            $script:testsFailed++
        }
    }
    
    Write-Host ""
    Start-Sleep -Milliseconds 500
}

# 測試 1: 缺少 Token Header
Test-ApiEndpoint `
    -TestName "缺少 Token Header - 應拒絕存取" `
    -Url "$baseUrl/api/protected" `
    -Headers @{} `
    -Body '{"data":"測試資料"}' `
    -ExpectedStatusCode 401 `
    -Description "驗證 API 是否拒絕未攜帶 Token 的請求"

# 測試 2: 使用無效 Token
Test-ApiEndpoint `
    -TestName "使用無效/偽造的 Token - 應拒絕存取" `
    -Url "$baseUrl/api/protected" `
    -Headers @{ "X-CSRF-Token" = "fake-invalid-token-12345" } `
    -Body '{"data":"測試資料"}' `
    -ExpectedStatusCode 401 `
    -Description "防止攻擊者使用偽造 Token 繞過驗證"

# 測試 3-5: 取得 Token 並測試使用次數
Write-Host "測試: 取得 Token 並測試使用次數限制" -ForegroundColor Yellow
Write-Host "說明: 驗證 Token 正常流程與使用次數機制" -ForegroundColor Gray

try {
    # 取得 Token
    $response = Invoke-WebRequest -Uri "$baseUrl/api/token?maxUsage=2&expirationMinutes=5" -Method Get
    $token = $response.Headers['X-CSRF-Token'][0]
    
    if ([string]::IsNullOrEmpty($token)) {
        Write-Host "✗ 無法取得 Token" -ForegroundColor Red
        $testsFailed += 3
    } else {
        Write-Host "✓ Token 取得成功: $token" -ForegroundColor Green
        Write-Host ""
        
        # 測試 3: 首次使用 Token
        Test-ApiEndpoint `
            -TestName "使用有效 Token (首次使用) - 應允許存取" `
            -Url "$baseUrl/api/protected" `
            -Headers @{ "X-CSRF-Token" = $token } `
            -Body '{"data":"測試資料第一次"}' `
            -ExpectedStatusCode 200 `
            -Description "驗證正常的 Token 使用流程"
        
        # 測試 4: 第二次使用 Token
        Test-ApiEndpoint `
            -TestName "Token 重複使用 (第二次) - 應允許" `
            -Url "$baseUrl/api/protected" `
            -Headers @{ "X-CSRF-Token" = $token } `
            -Body '{"data":"測試資料第二次"}' `
            -ExpectedStatusCode 200 `
            -Description "驗證 Token 使用次數計數機制 (maxUsage=2)"
        
        # 測試 5: 第三次使用 Token (超過限制)
        Test-ApiEndpoint `
            -TestName "Token 超過使用次數限制 (第三次) - 應拒絕" `
            -Url "$baseUrl/api/protected" `
            -Headers @{ "X-CSRF-Token" = $token } `
            -Body '{"data":"測試資料第三次"}' `
            -ExpectedStatusCode 401 `
            -Description "防止 Token 被無限次重複使用"
    }
} catch {
    Write-Host "✗ Token 測試流程失敗: $_" -ForegroundColor Red
    $testsFailed += 3
    Write-Host ""
}

# 測試 6: 使用過期的 Token
Write-Host "測試: 使用過期的 Token - 應拒絕存取" -ForegroundColor Yellow
Write-Host "說明: 驗證 Token 過期機制" -ForegroundColor Gray

try {
    # 取得短效 Token (1 秒過期)
    $response = Invoke-WebRequest -Uri "$baseUrl/api/token?maxUsage=5&expirationMinutes=1" -Method Get
    $expiredToken = $response.Headers['X-CSRF-Token'][0]
    
    if ([string]::IsNullOrEmpty($expiredToken)) {
        Write-Host "✗ 無法取得短效 Token" -ForegroundColor Red
        $testsFailed++
    } else {
        Write-Host "等待 Token 過期 (65 秒)..." -ForegroundColor Gray
        Start-Sleep -Seconds 65
        
        Test-ApiEndpoint `
            -TestName "使用過期 Token - 應拒絕" `
            -Url "$baseUrl/api/protected" `
            -Headers @{ "X-CSRF-Token" = $expiredToken } `
            -Body '{"data":"測試過期Token"}' `
            -ExpectedStatusCode 401 `
            -Description "確保過期 Token 無法被使用"
    }
} catch {
    Write-Host "✗ 過期 Token 測試失敗: $_" -ForegroundColor Red
    $testsFailed++
    Write-Host ""
}

# 測試 7: 空白 Token Header
Test-ApiEndpoint `
    -TestName "空白 Token Header - 應拒絕存取" `
    -Url "$baseUrl/api/protected" `
    -Headers @{ "X-CSRF-Token" = "" } `
    -Body '{"data":"測試資料"}' `
    -ExpectedStatusCode 401 `
    -Description "防止攻擊者使用空值繞過驗證"

# 測試結果總覽
Write-Host "=== 測試結果總覽 ===" -ForegroundColor Cyan
Write-Host "通過: $testsPassed 項" -ForegroundColor Green
Write-Host "失敗: $testsFailed 項" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "✓ 所有安全性測試通過！API 受到適當保護。" -ForegroundColor Green
} else {
    Write-Host "✗ 部分測試失敗，請檢查 API 安全性設定。" -ForegroundColor Red
    exit 1
}
