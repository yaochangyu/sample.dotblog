#!/usr/bin/env bash
# Open Design - 把這個專案目前的所有檔案同步到 sample.dotblog repo 裡的 OpenDesign 子資料夾。
#
# Mirror 同步：先清空 DEST_DIR，再用 cp -a 整份複製過去（保留權限與隱藏檔案），
# 確保 DEST 不會殘留 SRC 已經刪除的舊檔案。清空前一定會要求輸入 y 才繼續，
# 其他任何輸入（包含直接按 Enter）都會中止，不執行清空。
#
# 用法：
#   ./sync-file.sh
set -euo pipefail

SRC_DIR="$(cd "$(dirname "$0")" && pwd)"
DEST_DIR="/mnt/d/lab/sample.dotblog/OpenDesign"

# ── 防呆：避免 DEST_DIR 是空字串或 "/" 時誤刪整個磁碟 ──────────────────────
if [ -z "$DEST_DIR" ] || [ "$DEST_DIR" = "/" ]; then
  echo "ERROR: DEST_DIR 不合法（空字串或 /），中止。" >&2
  exit 1
fi

# ── 清空前互動確認（清除檔案是破壞性操作，一定要先問過再動手）─────────────
echo "即將清空目標資料夾：${DEST_DIR}"
read -r -p "確定要繼續嗎？輸入 y 才會清空並同步，其他任何輸入都會中止 [y/N] " confirm
if [ "$confirm" != "y" ]; then
  echo "已取消，沒有任何檔案被刪除。"
  exit 1
fi

rm -rf "$DEST_DIR"
mkdir -p "$DEST_DIR"
cp -a "$SRC_DIR/." "$DEST_DIR/"

echo "已同步（mirror）：${SRC_DIR} -> ${DEST_DIR}"
diff -rq "$SRC_DIR" "$DEST_DIR" --exclude=sync-file.sh \
  && echo "內容一致。" \
  || echo "上面列出的是差異（若只有 sync-file.sh 本身不一致是正常的，DEST 沒有這支腳本）。"
