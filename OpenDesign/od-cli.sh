#!/usr/bin/env bash
# Open Design - 統一管理入口
#
# 用法：
#   ./od-cli.sh start    背景啟動 daemon + web（已啟動則略過）
#   ./od-cli.sh stop     關閉 daemon + web
#   ./od-cli.sh update   git pull 取最新版 + 視情況重裝依賴 + rebuild daemon
set -euo pipefail

# ── 設定 ──────────────────────────────────────────────────────────────────
DAEMON_PORT=7456
WEB_PORT=3000
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="${SCRIPT_DIR}/open-design"

usage() {
  cat <<EOF
用法：
  $0 start    背景啟動 daemon + web（已啟動則略過）
  $0 stop     關閉 daemon + web
  $0 update   git pull 取最新版 + 視情況重裝依賴 + rebuild daemon
EOF
}

# ── 共用工具 ──────────────────────────────────────────────────────────────

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

load_nvm() {
  export NVM_DIR="$HOME/.nvm"
  if [ ! -s "$NVM_DIR/nvm.sh" ]; then
    echo "ERROR: nvm 未安裝，請先執行 nvm 安裝腳本。" >&2
    exit 1
  fi
  # nvm 內部有 unbound variable，暫時關閉 -u
  set +u
  source "$NVM_DIR/nvm.sh"
  nvm use 24 --silent
  set -u
}

# ════════════════════════════════════════════════════════════════════════
# start：背景啟動 daemon + web
# ════════════════════════════════════════════════════════════════════════

start_cmd() {
  load_nvm

  if [ ! -d "$REPO_DIR" ]; then
    echo "ERROR: repo 不存在：${REPO_DIR}，請先 clone 並執行 pnpm install。" >&2
    exit 1
  fi

  cd "$REPO_DIR"

  # ── daemon ──────────────────────────────────────────────────────────────
  if daemon_is_running; then
    echo "Daemon already running on port ${DAEMON_PORT}"
  else
    echo "Starting daemon on port ${DAEMON_PORT}..."

    # OD_WEB_PORT 告訴 daemon 信任來自 Next.js dev server（port 3000）的請求（否則回傳 403）
    OD_WEB_PORT="${WEB_PORT}" node apps/daemon/dist/cli.js --port "${DAEMON_PORT}" --no-open \
      > /tmp/od-daemon.log 2>&1 &
    DAEMON_PID=$!
    echo "Daemon PID: ${DAEMON_PID}"

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

  # ── web ─────────────────────────────────────────────────────────────────
  if web_is_running; then
    echo "Web already running on port ${WEB_PORT}"
  else
    if ss -tlnp | grep -q ":${WEB_PORT}"; then
      echo "Port ${WEB_PORT} is in use by another process, killing it..."
      fuser -k "${WEB_PORT}/tcp" 2>/dev/null || true
      sleep 1
    fi

    echo "Starting web on port ${WEB_PORT}..."

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

# ════════════════════════════════════════════════════════════════════════
# stop：關閉 daemon + web
# ════════════════════════════════════════════════════════════════════════

stop_cmd() {
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
# update：git pull + pnpm install（lockfile 有變）+ rebuild daemon
# ════════════════════════════════════════════════════════════════════════

update_cmd() {
  load_nvm

  if [ ! -d "$REPO_DIR" ]; then
    echo "ERROR: repo 不存在：${REPO_DIR}" >&2
    exit 1
  fi

  # ── 1. 更新 od-cli.sh 所在的 repo（sample.dotblog）──────────────────────
  WRAPPER_GIT_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel 2>/dev/null || echo "")"
  if [ -n "$WRAPPER_GIT_ROOT" ]; then
    echo "Pulling wrapper repo (${WRAPPER_GIT_ROOT})..."
    git -C "$WRAPPER_GIT_ROOT" pull
  fi

  # ── 2. 更新 open-design 原始碼 ───────────────────────────────────────────
  echo ""
  echo "Pulling open-design..."
  cd "$REPO_DIR"
  # 記錄 pull 前的 lockfile blob hash，pull 後比對決定是否重裝依賴
  LOCK_BEFORE="$(git rev-parse HEAD:pnpm-lock.yaml 2>/dev/null || echo "")"
  git pull
  LOCK_AFTER="$(git rev-parse HEAD:pnpm-lock.yaml 2>/dev/null || echo "")"

  if [ "$LOCK_BEFORE" != "$LOCK_AFTER" ]; then
    echo "pnpm-lock.yaml changed, running pnpm install..."
    # postinstall 會自動 rebuild 所有內部套件（含 daemon）
    pnpm install
  else
    echo "Rebuilding daemon..."
    pnpm --filter @open-design/daemon build
  fi

  echo ""
  echo "Update complete. 執行 '$0 start' 啟動。"
}

# ════════════════════════════════════════════════════════════════════════
# 入口
# ════════════════════════════════════════════════════════════════════════

cmd="${1:-}"

case "$cmd" in
  start)  start_cmd ;;
  stop)   stop_cmd ;;
  update) update_cmd ;;
  *)
    usage
    exit 1
    ;;
esac
