# 測試案例 5.4: 缺少 User-Agent Header
# 目的: 驗證系統拒絕沒有 User-Agent 的請求

$API_BASE = "http://localhost:5073"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "測試 5.4: 缺少 User-Agent Header" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 測試 5.4-1: 嘗試在沒有 User-Agent 的情況下取得 Token
Write-Host "測試 5.4-1: 無 User-Agent 取得 Token" -ForegroundColor Yellow
Write-Host "說明: 應該被拒絕並回傳 400 Bad Request" -ForegroundColor Gray
Write-Host ""

try {
    # 使用 WebRequest 來完全控制 Headers，移除預設的 User-Agent
    $request = [System.Net.WebRequest]::Create("$API_BASE/api/token")
    $request.Method = "GET"
    $request.UserAgent = $null  # 明確設為 null
    
    # 移除所有預設 headers
    if ($request.Headers["User-Agent"]) {
        $request.Headers.Remove("User-Agent")
    }
    
    try {
        $response = $request.GetResponse()
        $stream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $responseBody = $reader.ReadToEnd()
        $statusCode = [int]$response.StatusCode
        
        Write-Host "❌ 測試失敗 - 應該被拒絕但卻成功" -ForegroundColor Red
        Write-Host "狀態碼: $statusCode" -ForegroundColor Red
        Write-Host "回應: $responseBody" -ForegroundColor Red
        
        $response.Close()
        return $false
    }
    catch [System.Net.WebException] {
        $errorResponse = $_.Exception.Response
        if ($errorResponse) {
            $statusCode = [int]$errorResponse.StatusCode
            $stream = $errorResponse.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorBody = $reader.ReadToEnd()
            
            if ($statusCode -eq 400) {
                Write-Host "✅ 測試通過 - 正確拒絕無 User-Agent 的請求" -ForegroundColor Green
                Write-Host "狀態碼: $statusCode Bad Request" -ForegroundColor Green
                Write-Host "錯誤訊息: $errorBody" -ForegroundColor Gray
                Write-Host ""
            }
            else {
                Write-Host "⚠️ 測試部分通過 - 請求被拒絕但狀態碼不正確" -ForegroundColor Yellow
                Write-Host "預期狀態碼: 400, 實際: $statusCode" -ForegroundColor Yellow
                Write-Host "錯誤訊息: $errorBody" -ForegroundColor Gray
                Write-Host ""
            }
            
            $errorResponse.Close()
        }
    }
}
catch {
    Write-Host "❌ 測試執行錯誤: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    return $false
}

# 測試 5.4-2: 嘗試在沒有 User-Agent 的情況下使用 Protected API
Write-Host "測試 5.4-2: 無 User-Agent 呼叫 Protected API" -ForegroundColor Yellow
Write-Host "說明: 應該被拒絕" -ForegroundColor Gray
Write-Host ""

try {
    # 先用正常 User-Agent 取得 Token
    $tokenResponse = Invoke-RestMethod -Uri "$API_BASE/api/token" `
                                      -Method Get `
                                      -Headers @{ "User-Agent" = "Test-Browser/1.0" }
    
    $token = $tokenResponse.token
    Write-Host "已取得 Token: $($token.Substring(0,8))..." -ForegroundColor Gray
    Write-Host ""
    
    # 使用 WebRequest 嘗試在沒有 User-Agent 的情況下呼叫 Protected API
    $request = [System.Net.WebRequest]::Create("$API_BASE/api/protected")
    $request.Method = "POST"
    $request.ContentType = "application/json"
    $request.UserAgent = $null
    
    # 移除預設 User-Agent
    if ($request.Headers["User-Agent"]) {
        $request.Headers.Remove("User-Agent")
    }
    
    # 加入 CSRF Token
    $request.Headers.Add("X-CSRF-Token", $token)
    
    # 準備 request body
    $body = '{"Data":"test without user-agent"}'
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
    $request.ContentLength = $bytes.Length
    
    $stream = $request.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    
    try {
        $response = $request.GetResponse()
        $responseStream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($responseStream)
        $responseBody = $reader.ReadToEnd()
        $statusCode = [int]$response.StatusCode
        
        Write-Host "❌ 測試失敗 - 應該被拒絕但卻成功" -ForegroundColor Red
        Write-Host "狀態碼: $statusCode" -ForegroundColor Red
        Write-Host "回應: $responseBody" -ForegroundColor Red
        Write-Host ""
        
        $response.Close()
        return $false
    }
    catch [System.Net.WebException] {
        $errorResponse = $_.Exception.Response
        if ($errorResponse) {
            $statusCode = [int]$errorResponse.StatusCode
            $errorStream = $errorResponse.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            
            if ($statusCode -eq 401 -or $statusCode -eq 403) {
                Write-Host "✅ 測試通過 - 正確拒絕無 User-Agent 的 Protected API 請求" -ForegroundColor Green
                Write-Host "狀態碼: $statusCode" -ForegroundColor Green
                Write-Host "錯誤訊息: $errorBody" -ForegroundColor Gray
                Write-Host ""
            }
            else {
                Write-Host "⚠️ 測試部分通過 - 請求被拒絕但狀態碼不符預期" -ForegroundColor Yellow
                Write-Host "狀態碼: $statusCode" -ForegroundColor Yellow
                Write-Host "錯誤訊息: $errorBody" -ForegroundColor Gray
                Write-Host ""
            }
            
            $errorResponse.Close()
        }
    }
}
catch {
    Write-Host "❌ 測試執行錯誤: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    return $false
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "測試 5.4 完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
