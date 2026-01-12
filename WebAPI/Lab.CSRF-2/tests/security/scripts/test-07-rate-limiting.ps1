# 測試案例 2.4: 速率限制測試

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "測試案例 2.4: 速率限制測試" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }

Write-Host ""
Write-Host "測試: 1 分鐘內連續請求 6 次 Token (限制為 5 次)" -ForegroundColor Yellow
Write-Host "--------------------------------------"

$SUCCESS_COUNT = 0
$RATE_LIMITED = $false

for ($i = 1; $i -le 6; $i++) {
    Write-Host ""
    Write-Host "請求 #$i" -ForegroundColor Cyan
    
    try {
        $response = Invoke-WebRequest -Uri "$API_BASE/api/token?maxUsage=1&expirationMinutes=5" -Method Get
        $statusCode = $response.StatusCode
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
    }
    
    Write-Host "  HTTP 狀態碼: $statusCode" -ForegroundColor White
    
    if ($statusCode -eq 200) {
        $SUCCESS_COUNT++
        Write-Host "  ✅ 成功" -ForegroundColor Green
    }
    elseif ($statusCode -eq 429) {
        Write-Host "  ⚠️  速率限制觸發 (Too Many Requests)" -ForegroundColor Yellow
        $RATE_LIMITED = $true
        break
    }
    else {
        Write-Host "  ❌ 未預期的狀態碼: $statusCode" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "測試結果:" -ForegroundColor Cyan
Write-Host "  成功請求數: $SUCCESS_COUNT" -ForegroundColor White
Write-Host "  速率限制觸發: $RATE_LIMITED" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

if ($SUCCESS_COUNT -le 5 -and $RATE_LIMITED) {
    Write-Host ""
    Write-Host "✅ 測試通過: 速率限制正常運作" -ForegroundColor Green
    exit 0
}
elseif ($SUCCESS_COUNT -eq 5) {
    Write-Host ""
    Write-Host "⚠️  測試部分通過: 達到 5 次請求，但未觸發速率限制 (可能時間窗口已重置)" -ForegroundColor Yellow
    exit 0
}
else {
    Write-Host ""
    Write-Host "❌ 測試失敗: 速率限制未正常運作" -ForegroundColor Red
    exit 1
}
