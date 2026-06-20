#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = [
#     "psutil",
# ]
# ///
# Open Design - 統一管理入口

import glob
import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.request
from datetime import datetime
from pathlib import Path

DAEMON_PORT = 7456
WEB_PORT    = 3000
SCRIPT_DIR  = Path(__file__).parent.resolve()
REPO_DIR    = SCRIPT_DIR / "open-design"
SCRIPT_NAME = Path(__file__).name
TMP_DIR     = Path(tempfile.gettempdir())
IS_WINDOWS  = platform.system() == "Windows"


# ── 共用工具 ─────────────────────────────────────────────────────────────────

def usage():
    print("用法：")
    print(f"  {SCRIPT_NAME} start     背景啟動 daemon + web（已啟動則略過）")
    print(f"  {SCRIPT_NAME} stop      關閉 daemon + web")
    print(f"  {SCRIPT_NAME} update    git pull 取最新版 + 視情況重裝依賴 + rebuild daemon")
    print(f"  {SCRIPT_NAME} status    顯示 daemon + web 執行狀態")
    print(f"  {SCRIPT_NAME} version   顯示 open-design 版本")
    print(f"  {SCRIPT_NAME} help      顯示此說明")


def find_exe(name: str) -> str:
    """跨平台找執行檔，Windows 上 shutil.which 會自動補 .cmd/.exe"""
    return shutil.which(name) or name


def load_nvm():
    """確保 node 24 已在 PATH 中"""
    if IS_WINDOWS:
        if not shutil.which("nvm"):
            print("ERROR: nvm 未安裝，請先安裝 nvm-windows。", file=sys.stderr)
            sys.exit(1)
        subprocess.run(["nvm", "use", "24"], shell=True, capture_output=True)
    else:
        nvm_dir = Path.home() / ".nvm"
        if not (nvm_dir / "nvm.sh").exists():
            print("ERROR: nvm 未安裝，請先執行 nvm 安裝腳本。", file=sys.stderr)
            sys.exit(1)
        dirs = sorted(glob.glob(str(nvm_dir / "versions" / "node" / "v24.*")))
        if not dirs:
            print("ERROR: nvm 中找不到 node v24，請先執行 nvm install 24。", file=sys.stderr)
            sys.exit(1)
        node_bin = str(Path(dirs[-1]) / "bin")
        os.environ["PATH"] = f"{node_bin}:{os.environ.get('PATH', '')}"


def check_zenity():
    """Linux/WSL2 上確認 zenity 已安裝，否則警告「選擇工作目錄」功能無法使用"""
    if IS_WINDOWS:
        return
    if shutil.which("zenity"):
        return
    print("⚠️  警告：zenity 未安裝，Open Design 的「選擇工作目錄」功能將無法使用。")
    print("   修復：sudo apt install -y zenity")
    print()


def get_port_pid(port: int) -> int | None:
    import psutil
    for conn in psutil.net_connections(kind="tcp"):
        if conn.laddr.port == port and conn.status == "LISTEN":
            return conn.pid
    return None


def daemon_is_running() -> bool:
    try:
        with urllib.request.urlopen(
            f"http://localhost:{DAEMON_PORT}/api/health", timeout=2
        ) as r:
            return json.loads(r.read()).get("ok") is True
    except Exception:
        return False


def web_is_running() -> bool:
    try:
        urllib.request.urlopen(f"http://localhost:{WEB_PORT}", timeout=2)
        return True
    except Exception:
        return False


def popen_background(args: list, log_path: Path, env=None, cwd=None) -> subprocess.Popen:
    log_file = open(log_path, "w", encoding="utf-8")
    kwargs: dict = dict(
        stdout=log_file,
        stderr=subprocess.STDOUT,
        env=env or os.environ.copy(),
        cwd=str(cwd) if cwd else None,
    )
    if IS_WINDOWS:
        kwargs["creationflags"] = (
            subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
        )
    else:
        kwargs["start_new_session"] = True
    return subprocess.Popen(args, **kwargs)


def wait_ready(check_fn, proc: subprocess.Popen, name: str, log_path: Path, timeout=30):
    for i in range(1, timeout + 1):
        if check_fn():
            print(f"{name} ready ({i}s).")
            return
        if proc.poll() is not None:
            print(f"ERROR: {name} process exited unexpectedly. Check {log_path}", file=sys.stderr)
            print(log_path.read_text(encoding="utf-8", errors="replace"), file=sys.stderr)
            sys.exit(1)
        print(f"Waiting for {name}... ({i}/{timeout})")
        time.sleep(1)
    print(f"ERROR: {name} failed to respond within {timeout}s. Check {log_path}", file=sys.stderr)
    print(log_path.read_text(encoding="utf-8", errors="replace"), file=sys.stderr)
    sys.exit(1)


# ════════════════════════════════════════════════════════════════════════
# start
# ════════════════════════════════════════════════════════════════════════

def start_cmd():
    load_nvm()
    check_zenity()

    if not REPO_DIR.exists():
        print(f"ERROR: repo 不存在：{REPO_DIR}，請先 clone 並執行 pnpm install。", file=sys.stderr)
        sys.exit(1)

    # ── daemon ──────────────────────────────────────────────────────────
    if daemon_is_running():
        print(f"Daemon already running on port {DAEMON_PORT}")
    else:
        print(f"Starting daemon on port {DAEMON_PORT}...")
        log = TMP_DIR / "od-daemon.log"
        env = {**os.environ, "OD_WEB_PORT": str(WEB_PORT)}
        proc = popen_background(
            [find_exe("node"), "apps/daemon/dist/cli.js", "--port", str(DAEMON_PORT), "--no-open"],
            log, env=env, cwd=REPO_DIR,
        )
        print(f"Daemon PID: {proc.pid}")
        wait_ready(daemon_is_running, proc, "Daemon", log)

    print()

    # ── web ─────────────────────────────────────────────────────────────
    if web_is_running():
        print(f"Web already running on port {WEB_PORT}")
    else:
        pid = get_port_pid(WEB_PORT)
        if pid:
            print(f"Port {WEB_PORT} is in use by another process, killing it...")
            import psutil
            psutil.Process(pid).kill()
            time.sleep(1)

        print(f"Starting web on port {WEB_PORT}...")
        log = TMP_DIR / "od-web.log"
        env = {**os.environ, "PORT": str(WEB_PORT), "OD_PORT": str(DAEMON_PORT)}
        proc = popen_background(
            [find_exe("pnpm"), "--filter", "@open-design/web", "dev"],
            log, env=env, cwd=REPO_DIR,
        )
        print(f"Web PID: {proc.pid}")
        wait_ready(web_is_running, proc, "Web", log)

    print()
    print(f"Open: http://localhost:{WEB_PORT}")
    print(f"用 '{SCRIPT_NAME} stop' 可關閉 daemon + web。")


# ════════════════════════════════════════════════════════════════════════
# stop
# ════════════════════════════════════════════════════════════════════════

def stop_cmd():
    import psutil
    pairs = [("daemon", DAEMON_PORT), ("web", WEB_PORT)]
    any_stopped = False
    for name, port in pairs:
        pid = get_port_pid(port)
        if pid:
            print(f"關閉 {name}（port {port}, PID {pid}）...")
            try:
                psutil.Process(pid).terminate()
            except psutil.NoSuchProcess:
                pass
            any_stopped = True
        else:
            print(f"{name}（port {port}）本來就沒在跑。")
    if any_stopped:
        print("完成。")


# ════════════════════════════════════════════════════════════════════════
# update
# ════════════════════════════════════════════════════════════════════════

def update_cmd():
    load_nvm()

    if not REPO_DIR.exists():
        print(f"ERROR: repo 不存在：{REPO_DIR}", file=sys.stderr)
        sys.exit(1)

    # ── 1. 更新 od-cli.py 所在的 repo ────────────────────────────────────
    result = subprocess.run(
        ["git", "-C", str(SCRIPT_DIR), "rev-parse", "--show-toplevel"],
        capture_output=True, text=True,
    )
    if result.returncode == 0:
        wrapper_root = result.stdout.strip()
        print(f"Pulling wrapper repo ({wrapper_root})...")
        subprocess.run(["git", "-C", wrapper_root, "pull"], check=True)

    # ── 2. 更新 open-design 原始碼 ───────────────────────────────────────
    print()
    print("Pulling open-design...")

    def git_hash(ref: str) -> str:
        r = subprocess.run(
            ["git", "rev-parse", ref],
            capture_output=True, text=True, cwd=str(REPO_DIR),
        )
        return r.stdout.strip()

    lock_before = git_hash("HEAD:pnpm-lock.yaml")
    subprocess.run(["git", "pull"], check=True, cwd=str(REPO_DIR))
    lock_after = git_hash("HEAD:pnpm-lock.yaml")

    if lock_before != lock_after:
        print("pnpm-lock.yaml changed, running pnpm install...")
        subprocess.run([find_exe("pnpm"), "install"], check=True, cwd=str(REPO_DIR))
    else:
        print("Rebuilding daemon...")
        subprocess.run(
            [find_exe("pnpm"), "--filter", "@open-design/daemon", "build"],
            check=True, cwd=str(REPO_DIR),
        )

    print()
    print(f"Update complete. 執行 '{SCRIPT_NAME} start' 啟動。")


# ════════════════════════════════════════════════════════════════════════
# status
# ════════════════════════════════════════════════════════════════════════

def status_cmd():
    import psutil

    for name, port in [("daemon", DAEMON_PORT), ("web", WEB_PORT)]:
        pid = get_port_pid(port)
        if pid:
            try:
                proc = psutil.Process(pid)
                start_dt = datetime.fromtimestamp(proc.create_time())
                elapsed  = datetime.now() - start_dt
                h, rem   = divmod(int(elapsed.total_seconds()), 3600)
                m, s     = divmod(rem, 60)
                uptime   = f"{h:02d}:{m:02d}:{s:02d}"
                mem_mb   = proc.memory_info().rss / 1024 / 1024
                cpu      = proc.cpu_percent(interval=0.1)
                print(
                    f"{name:<8} [running]  port={port:<5} PID={pid:<6} "
                    f"uptime={uptime:<12} started={start_dt:%Y-%m-%d %H:%M:%S}"
                )
                print(f"{'':8}             mem={mem_mb:.1f}MB  cpu={cpu:.1f}%")
            except psutil.NoSuchProcess:
                print(f"{name:<8} [running]  port={port:<5} PID={pid:<6} (process not found)")
        else:
            print(f"{name:<8} [stopped]  port={port}")

    print()
    print("daemon health: ", end="", flush=True)
    try:
        with urllib.request.urlopen(
            f"http://localhost:{DAEMON_PORT}/api/health", timeout=2
        ) as r:
            print(r.read().decode().strip())
    except Exception:
        print("unreachable")


# ════════════════════════════════════════════════════════════════════════
# version
# ════════════════════════════════════════════════════════════════════════

def version_cmd():
    pkg = REPO_DIR / "package.json"
    if not pkg.exists():
        print(f"ERROR: 找不到 {pkg}", file=sys.stderr)
        sys.exit(1)
    print(json.loads(pkg.read_text(encoding="utf-8"))["version"])


# ════════════════════════════════════════════════════════════════════════
# 入口
# ════════════════════════════════════════════════════════════════════════

COMMANDS = {
    "start":   start_cmd,
    "stop":    stop_cmd,
    "update":  update_cmd,
    "status":  status_cmd,
    "version": version_cmd,
    "help":    usage,
    "--help":  usage,
}

cmd = sys.argv[1] if len(sys.argv) > 1 else ""
fn  = COMMANDS.get(cmd)
if fn:
    fn()
else:
    usage()
    sys.exit(1)
