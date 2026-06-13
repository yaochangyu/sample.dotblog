#!/bin/bash

# 設定獨立 Ollama 連接埠與優化參數
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
    ollama serve > ollama_11435.log 2>&1 &
    # 等待服務啟動
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
    if [ -f Modelfile ]; then
        OLLAMA_HOST="127.0.0.1:11435" ollama create gemma4-opt -f Modelfile
    else
        echo "錯誤：找不到 Modelfile 檔案，無法建立優化模型。"
        exit 1
    fi
else
    echo "[2/3] 地端優化模型 gemma4-opt 已就緒。"
fi

# 3. 啟動 LiteLLM 服務
if ! lsof -i :4000 >/dev/null 2>&1; then
    echo "[3/3] 正在背景啟動 LiteLLM Proxy 代理 (Port: 4000)..."
    if [ -f litellm_config.yaml ]; then
        litellm --config litellm_config.yaml --port 4000 > litellm.log 2>&1 &
        sleep 3
    else
        echo "錯誤：找不到 litellm_config.yaml 檔案，無法啟動 LiteLLM。"
        exit 1
    fi
else
    echo "[3/3] LiteLLM 代理服務已在 Port 4000 運行中。"
fi

echo "----------------------------------------------------------"
echo "✨ 所有地端優化服務已成功於背景啟動！"
echo "請複製並在您的終端機執行以下命令，以連線地端執行 Claude Code："
echo ""
echo "export ANTHROPIC_BASE_URL=\"http://localhost:4000\""
echo "export ANTHROPIC_API_KEY=\"local-bypass\""
echo "export ANTHROPIC_CUSTOM_MODEL_OPTION=\"gemma4-opt\""
echo "export ANTHROPIC_CUSTOM_MODEL_OPTION_NAME=\"Gemma 4 e4b (Optimized)\""
echo "claude --model gemma4-opt"
echo "----------------------------------------------------------"
