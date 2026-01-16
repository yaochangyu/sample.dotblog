#!/bin/bash
# GitLab CLI 便捷執行腳本
# 使用方式: ./run-gl-cli.sh <command> [arguments...]

set -e  # 遇到錯誤立即退出

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 輔助函數
print_info() {
    echo -e "${BLUE}ℹ ${NC}$1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_header() {
    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}========================================${NC}\n"
}

# 檢查 UV 是否安裝
if ! command -v uv &> /dev/null; then
    print_error "UV 未安裝！"
    echo ""
    echo "請先安裝 UV："
    echo ""
    echo "  # Windows (PowerShell)"
    echo "  powershell -c \"irm https://astral.sh/uv/install.ps1 | iex\""
    echo ""
    echo "  # macOS/Linux"
    echo "  curl -LsSf https://astral.sh/uv/install.sh | sh"
    echo ""
    exit 1
fi

# 檢查是否在正確的目錄
if [ ! -f "gl-cli.py" ]; then
    print_error "請在 scripts 目錄下執行此腳本"
    exit 1
fi

# 檢查 config.py 是否存在
if [ ! -f "config.py" ]; then
    print_error "找不到 config.py，請先設定配置檔"
    exit 1
fi

# 顯示幫助訊息
show_help() {
    print_header "GitLab CLI 便捷執行腳本"
    
    echo "使用方式: ./run-gl-cli.sh <command> [arguments...]"
    echo ""
    echo "可用命令："
    echo ""
    echo "  1. 專案資訊查詢："
    echo "     ./run-gl-cli.sh project-stats [--project-name NAME] [--group-id ID]"
    echo ""
    echo "  2. 專案授權查詢："
    echo "     ./run-gl-cli.sh project-permission [--project-name NAME] [--group-id ID]"
    echo ""
    echo "  3. 使用者統計查詢："
    echo "     ./run-gl-cli.sh user-details [--username NAME] [--start-date YYYY-MM-DD] [--end-date YYYY-MM-DD] [--group-id ID]"
    echo ""
    echo "快速範例："
    echo ""
    echo "  # 取得所有專案資訊"
    echo "  ./run-gl-cli.sh project-stats"
    echo ""
    echo "  # 取得特定專案授權"
    echo "  ./run-gl-cli.sh project-permission --project-name \"my-project\""
    echo ""
    echo "  # 分析 2024 年的使用者活動"
    echo "  ./run-gl-cli.sh user-details --start-date 2024-01-01 --end-date 2024-12-31"
    echo ""
    echo "  # 分析特定使用者"
    echo "  ./run-gl-cli.sh user-details --username johndoe"
    echo ""
    echo "其他命令："
    echo ""
    echo "  ./run-gl-cli.sh help          - 顯示此幫助訊息"
    echo "  ./run-gl-cli.sh sync          - 同步/安裝相依套件"
    echo "  ./run-gl-cli.sh clean         - 清理輸出目錄"
    echo ""
}

# 同步相依套件
sync_dependencies() {
    print_header "同步相依套件"
    print_info "執行 uv sync..."
    uv sync
    print_success "相依套件同步完成"
}

# 清理輸出目錄
clean_output() {
    print_header "清理輸出目錄"
    
    if [ -d "output" ]; then
        read -p "確定要刪除 output 目錄中的所有檔案嗎? (y/N) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            rm -rf output/*
            print_success "輸出目錄已清理"
        else
            print_warning "已取消"
        fi
    else
        print_warning "輸出目錄不存在"
    fi
}

# 執行 CLI
run_cli() {
    print_header "執行 GitLab CLI"
    
    print_info "命令: gl-cli.py $@"
    echo ""
    
    # 執行 CLI
    uv run python gl-cli.py "$@"
    
    local exit_code=$?
    
    echo ""
    if [ $exit_code -eq 0 ]; then
        print_success "執行完成"
        echo ""
        print_info "輸出檔案位於: ./output/"
        
        # 列出產生的檔案
        if [ -d "output" ] && [ "$(ls -A output)" ]; then
            echo ""
            echo "產生的檔案："
            ls -lh output/ | tail -n +2 | awk '{printf "  - %s (%s)\n", $9, $5}'
        fi
    else
        print_error "執行失敗 (退出碼: $exit_code)"
        exit $exit_code
    fi
}

# 主邏輯
main() {
    # 如果沒有參數，顯示幫助
    if [ $# -eq 0 ]; then
        show_help
        exit 0
    fi
    
    # 解析命令
    case "$1" in
        help|--help|-h)
            show_help
            ;;
        sync)
            sync_dependencies
            ;;
        clean)
            clean_output
            ;;
        project-stats|project-permission|user-details)
            run_cli "$@"
            ;;
        *)
            print_error "未知命令: $1"
            echo ""
            echo "執行 './run-gl-cli.sh help' 查看可用命令"
            exit 1
            ;;
    esac
}

# 執行主程式
main "$@"
