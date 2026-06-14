#!/bin/bash

echo "=========================================================="
echo "         Gemma 4 E4B + LiteLLM 環境安裝腳本"
echo "=========================================================="

# 1. 安裝 Ollama
if ! command -v ollama &>/dev/null; then
    echo "[1/3] 安裝 Ollama..."
    curl -fsSL https://ollama.com/install.sh | sh
else
    echo "[1/3] Ollama 已安裝，跳過。"
fi

# 2. 拉取 gemma4:e4b 模型
echo "[2/3] 拉取 gemma4:e4b 模型（約 3.3 GB，首次執行需等待）..."
ollama pull gemma4:e4b

# 3. 安裝 LiteLLM Proxy
if ! command -v litellm &>/dev/null; then
    echo "[3/3] 安裝 LiteLLM Proxy..."
    pip install 'litellm[proxy]' --break-system-packages
else
    echo "[3/3] LiteLLM 已安裝，跳過。"
fi

echo "----------------------------------------------------------"
echo "✨ 安裝完成！接下來執行："
echo "   bash script/start-optimized-env.sh"
echo "----------------------------------------------------------"
