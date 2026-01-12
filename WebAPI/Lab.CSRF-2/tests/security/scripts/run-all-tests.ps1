# 執行所有安全性測試

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  執行所有安全性測試" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$API_BASE = if ($env:API_BASE) { $env:API_BASE } else { "http://localhost:5073" }
$env:API_BASE = $API_BASE

$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$TOTAL = 0
$PASSED = 0
$FAILED = 0

# 測試腳本列表
$tests = @(
    @{Script = "test-01-normal-flow.ps1"; Name = "正常流程測試"}
    @{Script = "test-02-token-expiration.ps1"; Name = "Token 過期測試"}
    @{Script = "test-03-usage-limit.ps1"; Name = "使用次數限制測試"}
    @{Script = "test-04-missing-token.ps1"; Name = "無 Token 測試"}
    @{Script = "test-05-invalid-token.ps1"; Name = "無效 Token 測試"}
    @{Script = "test-06-ua-mismatch.ps1"; Name = "User-Agent 不一致測試"}
    @{Script = "test-07-rate-limiting.ps1"; Name = "速率限制測試"}
    @{Script = "test-11-direct-attack.ps1"; Name = "直接攻擊測試"}
    @{Script = "test-12-replay-attack.ps1"; Name = "重放攻擊測試"}
)

Write-Host "API 伺服器: $API_BASE" -ForegroundColor White
Write-Host "測試腳本數量: $($tests.Count)" -ForegroundColor White
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

foreach ($test in $tests) {
    $TOTAL++
    
    Write-Host "[$TOTAL/$($tests.Count)] 執行: $($test.Name)" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    try {
        $result = & "$SCRIPT_DIR\$($test.Script)"
        if ($LASTEXITCODE -eq 0) {
            $PASSED++
            Write-Host ""
        } else {
            $FAILED++
            Write-Host ""
        }
    }
    catch {
        $FAILED++
        Write-Host "錯誤: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
    
    # 測試之間暫停，避免速率限制影響
    if ($test.Script -ne "test-12-replay-attack.ps1") {
        Start-Sleep -Seconds 2
    }
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  測試結果統計" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "總測試數: $TOTAL" -ForegroundColor White
Write-Host "通過: $PASSED" -ForegroundColor Green
Write-Host "失敗: $FAILED" -ForegroundColor $(if ($FAILED -eq 0) { "Green" } else { "Red" })
Write-Host "==========================================" -ForegroundColor Cyan

if ($FAILED -eq 0) {
    Write-Host ""
    Write-Host "✅ 所有測試通過！" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "❌ 有 $FAILED 個測試失敗" -ForegroundColor Red
    exit 1
}
