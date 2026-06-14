#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export OLLAMA_HOST="127.0.0.1:11435"
export OLLAMA_NUM_PARALLEL=1
export OLLAMA_MAX_LOADED_MODELS=1
export OLLAMA_FLASH_ATTENTION=1

echo "=========================================================="
echo "         地端 Ollama + LiteLLM 一鍵優化啟動腳本"
echo "=========================================================="

# 1. 啟動獨立 Ollama 服務
if ! lsof -i :11435 >/dev/null 2>&1; then
    echo "[1/3] 正在背景啟動獨立 Ollama 服務 (Port: 11435)..."
    ollama serve > "$SCRIPT_DIR/ollama_11435.log" 2>&1 &
    for i in {1..10}; do
        if curl -s http://127.0.0.1:11435 >/dev/null; then
            break
        fi
        sleep 1
    done
else
    echo "[1/3] Ollama 服務已在 Port 11435 運行中。"
fi

# 2. 檢查並自動建立 gemma4-opt 模型
if ! OLLAMA_HOST="127.0.0.1:11435" ollama list | grep -q "gemma4-opt"; then
    echo "[2/3] 地端模型 gemma4-opt 不存在，正在自動建立..."
    if [ -f "$SCRIPT_DIR/Modelfile" ]; then
        OLLAMA_HOST="127.0.0.1:11435" ollama create gemma4-opt -f "$SCRIPT_DIR/Modelfile"
    else
        echo "錯誤：找不到 Modelfile，無法建立優化模型。"
        exit 1
    fi
else
    echo "[2/3] 地端優化模型 gemma4-opt 已就緒。"
fi

# 3. 啟動 LiteLLM 服務
if ! lsof -i :4000 >/dev/null 2>&1; then
    echo "[3/3] 正在背景啟動 LiteLLM Proxy (Port: 4000)..."
    if [ -f "$SCRIPT_DIR/litellm_config.yaml" ]; then
        litellm --config "$SCRIPT_DIR/litellm_config.yaml" --port 4000 > "$SCRIPT_DIR/litellm.log" 2>&1 &
        sleep 3
    else
        echo "錯誤：找不到 litellm_config.yaml，無法啟動 LiteLLM。"
        exit 1
    fi
else
    echo "[3/3] LiteLLM 代理服務已在 Port 4000 運行中。"
fi

echo "----------------------------------------------------------"
echo "✨ 所有地端優化服務已成功啟動！"
echo "請執行以下指令啟動 Claude Code："
echo ""
echo "   bash script/run-claude.sh"
echo "----------------------------------------------------------"
