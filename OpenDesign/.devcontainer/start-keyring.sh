#!/usr/bin/env bash
# Open Design - Docker 模式：模擬桌面 keyring 環境
#
# Antigravity CLI 的登入憑證透過系統 keyring（libsecret）儲存，
# 容器內預設沒有 dbus session 與 keyring daemon，需手動起一組常駐的：
#   1. dbus session bus
#   2. gnome-keyring-daemon（建立一個無密碼的 login keyring）
#
# 此腳本具備 idempotent 檢查（透過 KEYRING_ENV_FILE），
# 可在每個新開的互動式 shell 重複 source，但只會真正啟動一次背景程序。
#
# 注意：此為 best-effort 做法（官方文件未明確說明容器無 keyring 時的行為），
# 實際是否能讓 Antigravity 登入流程正常運作，需在 Step 7 build 後實測驗證。

KEYRING_ENV_FILE="/tmp/.keyring-env"

if [ ! -f "$KEYRING_ENV_FILE" ]; then
  eval "$(dbus-launch --sh-syntax)"
  {
    echo "export DBUS_SESSION_BUS_ADDRESS='${DBUS_SESSION_BUS_ADDRESS}'"
    echo "export DBUS_SESSION_BUS_PID='${DBUS_SESSION_BUS_PID}'"
  } > "$KEYRING_ENV_FILE"

  # 用空密碼建立/解鎖 login keyring（headless 常用手法）
  eval "$(printf '\n' | gnome-keyring-daemon --unlock --components=secrets,pkcs11,ssh)"
  {
    echo "export GNOME_KEYRING_CONTROL='${GNOME_KEYRING_CONTROL}'"
    echo "export SSH_AUTH_SOCK='${SSH_AUTH_SOCK}'"
  } >> "$KEYRING_ENV_FILE"
fi

# shellcheck disable=SC1090
source "$KEYRING_ENV_FILE"
