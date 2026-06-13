#!/bin/bash
cd ~/projects/Local-LLM
./start-optimized-env.sh

# empty MCP config 停用所有 MCP servers，讓 tool 數從 59 降至 28
# 28 tools × ~3.3K chars = 23K tokens + 1.8K system + 4.4K msg = ~29K，恰好塞進 num_ctx=32768
env ANTHROPIC_BASE_URL="http://localhost:4000" \
    ANTHROPIC_API_KEY="local-bypass" \
    claude --model gemma4-opt \
    --mcp-config ~/projects/Local-LLM/empty-mcp.json \
    --strict-mcp-config
