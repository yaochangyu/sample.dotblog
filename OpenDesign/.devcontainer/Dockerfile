# Open Design - Docker 模式：內建四個 AI CLI（Claude / Copilot / Codex / Antigravity）
FROM mcr.microsoft.com/devcontainers/base:ubuntu

ARG NODE_VERSION=24

USER root

# ── 系統套件 ──────────────────────────────────────────────────────────────
# - curl/ca-certificates：各家 CLI 安裝腳本需要
# - gnome-keyring/dbus-x11/libsecret-1-0：Antigravity CLI 依賴系統 keyring（libsecret），
#   容器內預設沒有桌面 keyring daemon，需補裝並在啟動時起一個 dbus session 模擬
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    gnome-keyring \
    dbus-x11 \
    libsecret-1-0 \
    && rm -rf /var/lib/apt/lists/*

# ── Node.js（透過 nvm，與專案 INSTALL.md 一致：Node 24 + pnpm 10.33.2） ─────
# 安裝後建立一個固定路徑的 symlink（/usr/local/node-current），
# 避免 nvm 實際安裝的 patch 版本號（如 v24.3.0）造成 PATH 寫死版本號失效
ENV NVM_DIR=/usr/local/nvm
RUN mkdir -p "$NVM_DIR" \
    && curl -fsSL https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash \
    && . "$NVM_DIR/nvm.sh" \
    && nvm install ${NODE_VERSION} \
    && nvm alias default ${NODE_VERSION} \
    && corepack enable \
    && ln -s "$NVM_DIR/versions/node/$(nvm version default)" /usr/local/node-current \
    && chown -R vscode:vscode "$NVM_DIR"

ENV NODE_VERSION=${NODE_VERSION}
ENV PATH="/usr/local/node-current/bin:$PATH"

USER vscode
WORKDIR /home/vscode
ENV PATH="/home/vscode/.local/bin:$PATH"

# ── Step 2: Claude Code CLI ──────────────────────────────────────────────
# 來源：https://github.com/anthropics/claude-code
RUN curl -fsSL https://claude.ai/install.sh | bash

# ── Step 3: GitHub Copilot CLI ───────────────────────────────────────────
# 來源：https://docs.github.com/en/copilot/how-tos/copilot-cli/set-up-copilot-cli/install-copilot-cli
RUN npm install -g @github/copilot

# ── Step 4: OpenAI Codex CLI ──────────────────────────────────────────────
# 來源：https://github.com/openai/codex （注意 scope 必須是 @openai/codex，
# 誤打 `npm i -g codex` 會裝到完全不同的套件）
RUN npm install -g @openai/codex

# ── Step 5: Google Antigravity CLI ───────────────────────────────────────
# 來源：https://antigravity.google/download#antigravity-cli
# 安裝後二進位檔名稱是 `agy`（不是 antigravity），裝在 ~/.local/bin
RUN curl -fsSL https://antigravity.google/cli/install.sh | bash

# 預先建立所有 named volume 要掛載的目錄（以 vscode 身分），確保「全新空 volume」
# 第一次掛載時能正確從 image 繼承 vscode 屬主，而不是被 Docker 自動建立成 root 屬主。
# （.claude 因為 claude 安裝腳本本身會建立並寫入內容，不受影響；.copilot/.codex 安裝時
#  只放執行檔不會建立設定目錄，所以需要這裡補上）
# 詳見 .issues/docker-ai-cli.issues.md Issue 3。
RUN mkdir -p /home/vscode/.gemini/antigravity-cli /home/vscode/.local/share/keyrings \
    /home/vscode/.copilot /home/vscode/.codex

# ── keyring 啟動腳本：讓 Antigravity 的 libsecret 認證在容器內可用 ─────────
COPY --chown=vscode:vscode start-keyring.sh /home/vscode/.devcontainer-start-keyring.sh
RUN chmod +x /home/vscode/.devcontainer-start-keyring.sh \
    && echo 'source /home/vscode/.devcontainer-start-keyring.sh' >> /home/vscode/.bashrc
