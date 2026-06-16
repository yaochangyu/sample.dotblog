#!/usr/bin/env bash
# Open Design - 統一管理入口：Dev 模式啟停 + Docker AI CLI 容器
#
# 這支腳本同時管兩種完全不同的執行環境，使用前請先確認你要的是哪一種：
#   - Dev 模式（start/stop）：在 host 直接跑 Open Design 本身（daemon + web）。
#   - AI CLI 模式（build/shell/run）：每次都用 `docker run --rm` 跑一次性容器，
#     離開即自動清除（憑證存在 named volume，不需要常駐容器）。
#
# 用法：
#   ./od-cli.sh start                          背景啟動 Dev 模式（daemon + web，已啟動則略過）
#   ./od-cli.sh stop                           關閉 Dev 模式（daemon + web）
#   ./od-cli.sh build                          建置 AI CLI image
#   ./od-cli.sh shell                          一次性容器，互動式 bash（離開即自動清除）
#   ./od-cli.sh run <claude|copilot|codex|agy> 一次性容器，執行指定 CLI（離開即自動清除）
set -euo pipefail

# ── Dev 模式設定 ──────────────────────────────────────────────────────────
DAEMON_PORT=7456
WEB_PORT=3000
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="${SCRIPT_DIR}/open-design"

# ── AI CLI 模式設定 ───────────────────────────────────────────────────────
IMAGE="open-design-ai-cli:dev"
VALID_CLIS="claude copilot codex agy"

usage() {
  cat <<EOF
用法：
  $0 start                            背景啟動 Dev 模式（daemon + web，已啟動則略過）
  $0 stop                             關閉 Dev 模式（daemon + web）
  $0 build                            建置 AI CLI image
  $0 shell                            一次性容器，互動式 bash（離開即自動清除）
  $0 run <claude|copilot|codex|agy>   一次性容器，執行指定 CLI（離開即自動清除）
EOF
}

# ════════════════════════════════════════════════════════════════════════
# Dev 模式：host 直接啟動 Open Design（daemon + web）
# ════════════════════════════════════════════════════════════════════════

daemon_is_running() {
  curl -sf "http://localhost:${DAEMON_PORT}/api/health" 2>/dev/null \
    | grep -q '"ok":true'
}

web_is_running() {
  curl -sf "http://localhost:${WEB_PORT}" > /dev/null 2>&1
}

# 用 ss 反查目前監聽某個 port 的 process PID（找不到就印空字串）
port_pid() {
  # 加 `|| true`：set -e + pipefail 下，grep 找不到東西會讓整條 pipeline 回傳非 0，
  # 若不擋掉，呼叫端 `pid="$(port_pid ...)"` 這種單純賦值會直接觸發 set -e 中止整支腳本。
  local pid
  pid="$(ss -tlnp 2>/dev/null | grep -E ":$1[[:space:]]" | grep -oP 'pid=\K[0-9]+' | head -1 || true)"
  # ss 對不屬於目前使用者的 socket 不一定回得了 pid=（權限限制），退而用 lsof 再查一次。
  if [ -z "$pid" ]; then
    pid="$(lsof -t -iTCP:"$1" -sTCP:LISTEN 2>/dev/null | head -1 || true)"
  fi
  printf '%s' "$pid"
}

start_dev() {
  # ── 載入 nvm（nvm 內部有 unbound variable，暫時關閉 -u）──────────────────
  export NVM_DIR="$HOME/.nvm"
  if [ ! -s "$NVM_DIR/nvm.sh" ]; then
    echo "ERROR: nvm 未安裝，請先執行 nvm 安裝腳本。" >&2
    exit 1
  fi
  set +u
  source "$NVM_DIR/nvm.sh"

  if [ ! -d "$REPO_DIR" ]; then
    echo "ERROR: repo 不存在：${REPO_DIR}" >&2
    exit 1
  fi

  cd "$REPO_DIR"

  nvm use 24 --silent
  set -u

  # ── 更新 repo ────────────────────────────────────────────────────────────
  echo "Pulling latest changes..."
  git pull

  # 若 lockfile 有異動，重新安裝依賴
  if git diff HEAD@{1} --name-only 2>/dev/null | grep -q "pnpm-lock.yaml"; then
    echo "pnpm-lock.yaml changed, running pnpm install..."
    pnpm install
  fi

  # ── 確認 daemon 是否已在執行 ─────────────────────────────────────────────
  if daemon_is_running; then
    echo "Daemon already running on port ${DAEMON_PORT}"
  else
    echo "Starting daemon on port ${DAEMON_PORT}..."

    # 直接背景執行 node，取得真正的 node PID（不透過 nohup wrapper）
    # OD_WEB_PORT 告訴 daemon 信任來自 Next.js dev server 的請求（否則回傳 403）
    OD_WEB_PORT="${WEB_PORT}" node apps/daemon/dist/cli.js --port "${DAEMON_PORT}" --no-open \
      > /tmp/od-daemon.log 2>&1 &
    DAEMON_PID=$!
    echo "Daemon PID: ${DAEMON_PID}"

    # 等待 daemon 就緒（最多 30 秒，plugin 初始化需要時間）
    READY=0
    for i in $(seq 1 30); do
      if daemon_is_running; then
        echo "Daemon ready (${i}s)."
        READY=1
        break
      fi
      if ! kill -0 "${DAEMON_PID}" 2>/dev/null; then
        echo "ERROR: Daemon process exited unexpectedly. Check /tmp/od-daemon.log" >&2
        cat /tmp/od-daemon.log >&2
        exit 1
      fi
      echo "Waiting for daemon... (${i}/30)"
      sleep 1
    done

    if [ "${READY}" -eq 0 ]; then
      echo "ERROR: Daemon failed to respond within 30s. Check /tmp/od-daemon.log" >&2
      cat /tmp/od-daemon.log >&2
      exit 1
    fi
  fi

  echo ""

  # ── 確認 web 是否已在執行 ────────────────────────────────────────────────
  if web_is_running; then
    echo "Web already running on port ${WEB_PORT}"
  else
    # 確保 port 未被其他程式佔用
    if ss -tlnp | grep -q ":${WEB_PORT}"; then
      echo "Port ${WEB_PORT} is in use by another process, killing it..."
      fuser -k "${WEB_PORT}/tcp" 2>/dev/null || true
      sleep 1
    fi

    echo "Starting web on port ${WEB_PORT}..."

    # 跟 daemon 一樣背景執行（不再用前景 pnpm dev 佔住終端機），
    # 才能讓 'od-cli.sh start' 執行完直接返回，並讓 stop 之後能反查 port 關閉它。
    PORT=${WEB_PORT} OD_PORT=${DAEMON_PORT} pnpm --filter @open-design/web dev \
      > /tmp/od-web.log 2>&1 &
    WEB_PID=$!
    echo "Web PID: ${WEB_PID}"

    READY=0
    for i in $(seq 1 30); do
      if web_is_running; then
        echo "Web ready (${i}s)."
        READY=1
        break
      fi
      if ! kill -0 "${WEB_PID}" 2>/dev/null; then
        echo "ERROR: Web process exited unexpectedly. Check /tmp/od-web.log" >&2
        cat /tmp/od-web.log >&2
        exit 1
      fi
      echo "Waiting for web... (${i}/30)"
      sleep 1
    done

    if [ "${READY}" -eq 0 ]; then
      echo "ERROR: Web failed to respond within 30s. Check /tmp/od-web.log" >&2
      cat /tmp/od-web.log >&2
      exit 1
    fi
  fi

  echo ""
  echo "Open: http://localhost:${WEB_PORT}"
  echo "用 '$0 stop' 可關閉 daemon + web。"
}

stop_dev() {
  local any_stopped=0

  for pair in "daemon:${DAEMON_PORT}" "web:${WEB_PORT}"; do
    name="${pair%%:*}"
    port="${pair##*:}"
    pid="$(port_pid "$port")"
    if [ -n "$pid" ]; then
      echo "關閉 ${name}（port ${port}, PID ${pid}）..."
      kill "$pid" 2>/dev/null || true
      any_stopped=1
    else
      echo "${name}（port ${port}）本來就沒在跑。"
    fi
  done

  if [ "$any_stopped" -eq 1 ]; then
    echo "完成。"
  fi
}

# ════════════════════════════════════════════════════════════════════════
# AI CLI 模式：一次性 Docker 容器（每次 docker run --rm，不常駐）
# ════════════════════════════════════════════════════════════════════════

ensure_image() {
  if ! docker image inspect "$IMAGE" >/dev/null 2>&1; then
    echo "錯誤：image ${IMAGE} 不存在，請先執行：$0 build" >&2
    exit 1
  fi
}

# codex 的 OAuth callback server 只綁定容器內 127.0.0.1，單純 -p 轉送連不到，
# 改用 host 網路讓 loopback 直通（僅支援 Linux/WSL2）。
# 見 .issues/docker-ai-cli.issues.md Issue 5、Issue 6。
run_ephemeral() {
  ensure_image
  docker run --rm -it \
    --network host \
    -v open-design-claude-config:/home/vscode/.claude \
    -v open-design-copilot-config:/home/vscode/.copilot \
    -v open-design-codex-config:/home/vscode/.codex \
    -v open-design-antigravity-config:/home/vscode/.gemini/antigravity-cli \
    -v open-design-antigravity-keyring:/home/vscode/.local/share/keyrings \
    "$IMAGE" \
    bash -lc "${*:-bash}"
}

cmd="${1:-}"
[ $# -gt 0 ] && shift

case "$cmd" in
  start)
    start_dev
    ;;
  stop)
    stop_dev
    ;;
  build)
    docker build -t "$IMAGE" -f "$SCRIPT_DIR/.devcontainer/Dockerfile" "$SCRIPT_DIR/.devcontainer"
    ;;
  shell)
    run_ephemeral
    ;;
  run)
    target="${1:-}"
    if [ -z "$target" ]; then
      echo "錯誤：請指定要執行的 CLI（${VALID_CLIS}）" >&2
      usage
      exit 1
    fi
    run_ephemeral "$@"
    ;;
  *)
    usage
    exit 1
    ;;
esac
