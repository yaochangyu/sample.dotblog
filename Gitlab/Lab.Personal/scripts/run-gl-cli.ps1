# GitLab CLI 便捷執行腳本 (PowerShell)
# 使用方式: .\run-gl-cli.ps1 <command> [arguments...]

param(
    [Parameter(Position=0)]
    [string]$Command,
    
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

# 顏色函數
function Write-Info {
    param([string]$Message)
    Write-Host "ℹ " -ForegroundColor Blue -NoNewline
    Write-Host $Message
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Blue
    Write-Host "  $Message" -ForegroundColor Blue
    Write-Host "========================================" -ForegroundColor Blue
    Write-Host ""
}

# 檢查 UV 是否安裝
function Test-UvInstalled {
    try {
        $null = Get-Command uv -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

# 顯示幫助訊息
function Show-Help {
    Write-Header "GitLab CLI 便捷執行腳本"
    
    Write-Host "使用方式: .\run-gl-cli.ps1 <command> [arguments...]"
    Write-Host ""
    Write-Host "可用命令："
    Write-Host ""
    Write-Host "  1. 專案資訊查詢："
    Write-Host "     .\run-gl-cli.ps1 project-stats [-project-name NAME] [-group-id ID]"
    Write-Host ""
    Write-Host "  2. 專案授權查詢："
    Write-Host "     .\run-gl-cli.ps1 project-permission [-project-name NAME] [-group-id ID]"
    Write-Host ""
    Write-Host "  3. 使用者統計查詢:"
    Write-Host "     .\run-gl-cli.ps1 user-details [-username NAME] [-start-date YYYY-MM-DD] [-end-date YYYY-MM-DD] [-group-id ID]"
    Write-Host ""
    Write-Host "快速範例："
    Write-Host ""
    Write-Host "  # 取得所有專案資訊"
    Write-Host "  .\run-gl-cli.ps1 project-stats"
    Write-Host ""
    Write-Host "  # 取得特定專案授權"
    Write-Host '  .\run-gl-cli.ps1 project-permission --project-name "my-project"'
    Write-Host ""
    Write-Host "  # 分析 2024 年的使用者活動"
    Write-Host "  .\run-gl-cli.ps1 user-details --start-date 2024-01-01 --end-date 2024-12-31"
    Write-Host ""
    Write-Host "  # 分析特定使用者"
    Write-Host "  .\run-gl-cli.ps1 user-details --username johndoe"
    Write-Host ""
    Write-Host "其他命令："
    Write-Host ""
    Write-Host "  .\run-gl-cli.ps1 help          - 顯示此幫助訊息"
    Write-Host "  .\run-gl-cli.ps1 sync          - 同步/安裝相依套件"
    Write-Host "  .\run-gl-cli.ps1 clean         - 清理輸出目錄"
    Write-Host ""
}

# 同步相依套件
function Sync-Dependencies {
    Write-Header "同步相依套件"
    Write-Info "執行 uv sync..."
    
    uv sync
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "相依套件同步完成"
    } else {
        Write-Error "同步失敗"
        exit $LASTEXITCODE
    }
}

# 清理輸出目錄
function Clean-Output {
    Write-Header "清理輸出目錄"
    
    if (Test-Path "output") {
        $confirmation = Read-Host "確定要刪除 output 目錄中的所有檔案嗎? (y/N)"
        
        if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
            Remove-Item "output\*" -Recurse -Force
            Write-Success "輸出目錄已清理"
        } else {
            Write-Warning "已取消"
        }
    } else {
        Write-Warning "輸出目錄不存在"
    }
}

# 執行 CLI
function Invoke-Cli {
    param([string[]]$Args)
    
    Write-Header "執行 GitLab CLI"
    
    $cmdLine = "gl-cli.py " + ($Args -join " ")
    Write-Info "命令: $cmdLine"
    Write-Host ""
    
    # 執行 CLI
    $allArgs = @("run", "python", "gl-cli.py") + $Args
    & uv $allArgs
    
    $exitCode = $LASTEXITCODE
    
    Write-Host ""
    if ($exitCode -eq 0) {
        Write-Success "執行完成"
        Write-Host ""
        Write-Info "輸出檔案位於: .\output\"
        
        # 列出產生的檔案
        if (Test-Path "output" -and (Get-ChildItem "output").Count -gt 0) {
            Write-Host ""
            Write-Host "產生的檔案："
            Get-ChildItem "output" | ForEach-Object {
                $size = if ($_.Length -lt 1KB) {
                    "{0:N0} B" -f $_.Length
                } elseif ($_.Length -lt 1MB) {
                    "{0:N2} KB" -f ($_.Length / 1KB)
                } else {
                    "{0:N2} MB" -f ($_.Length / 1MB)
                }
                Write-Host "  - $($_.Name) ($size)"
            }
        }
    } else {
        Write-Error "執行失敗 (退出碼: $exitCode)"
        exit $exitCode
    }
}

# 主邏輯
function Main {
    # 檢查 UV 是否安裝
    if (-not (Test-UvInstalled)) {
        Write-Error "UV 未安裝！"
        Write-Host ""
        Write-Host "請先安裝 UV："
        Write-Host ""
        Write-Host "  # PowerShell"
        Write-Host '  powershell -c "irm https://astral.sh/uv/install.ps1 | iex"'
        Write-Host ""
        exit 1
    }
    
    # 檢查是否在正確的目錄
    if (-not (Test-Path "gl-cli.py")) {
        Write-Error "請在 scripts 目錄下執行此腳本"
        exit 1
    }
    
    # 檢查 config.py 是否存在
    if (-not (Test-Path "config.py")) {
        Write-Error "找不到 config.py，請先設定配置檔"
        exit 1
    }
    
    # 如果沒有參數，顯示幫助
    if (-not $Command) {
        Show-Help
        exit 0
    }
    
    # 解析命令
    switch ($Command) {
        { $_ -in "help", "--help", "-h" } {
            Show-Help
        }
        "sync" {
            Sync-Dependencies
        }
        "clean" {
            Clean-Output
        }
        { $_ -in "project-stats", "project-permission", "user-details" } {
            $allArgs = @($Command) + $Arguments
            Invoke-Cli -Args $allArgs
        }
        default {
            Write-Error "未知命令: $Command"
            Write-Host ""
            Write-Host "執行 '.\run-gl-cli.ps1 help' 查看可用命令"
            exit 1
        }
    }
}

# 執行主程式
Main
