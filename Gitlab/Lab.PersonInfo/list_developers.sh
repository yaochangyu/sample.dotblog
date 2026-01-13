#!/bin/bash

# 列出 Git 儲存庫中的所有開發者
# 用法: ./list_developers.sh [選項]

REPO_ROOT="/mnt/d/lab/sample.dotblog"

cd "$REPO_ROOT" || exit 1

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║           Git 儲存庫開發者列表                                ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# 方法 1: 依提交次數排序（最常用）
echo "📊 方法 1: 依提交次數排序"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
git shortlog -sn --all --no-merges | grep -v "bot"
echo ""

# 方法 2: 顯示名稱和 Email
echo "📧 方法 2: 開發者名稱與 Email"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
git log --all --format="%an|%ae" | sort -u | grep -v "bot" | column -t -s '|'
echo ""

# 方法 3: 詳細統計（含最後提交時間）
echo "📅 方法 3: 詳細統計（含最後提交時間）"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
printf "%-20s %-10s %-20s\n" "開發者" "提交次數" "最後提交"
printf "%-20s %-10s %-20s\n" "--------------------" "----------" "--------------------"

authors=$(git log --all --format="%an" | sort -u | grep -v "bot")

for author in $authors; do
  count=$(git log --all --author="$author" --oneline | wc -l)
  last_commit=$(git log --all --author="$author" --format="%ad" --date=short -1)
  printf "%-20s %-10s %-20s\n" "$author" "$count" "$last_commit"
done | sort -k2 -rn

echo ""

# 方法 4: 只列出名稱（適合腳本使用）
echo "📝 方法 4: 純名稱列表（適合複製貼上）"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
git log --all --format="%an" | sort -u | grep -v "bot"
echo ""

# 統計總結
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
total_devs=$(git log --all --format="%an" | sort -u | grep -v "bot" | wc -l)
total_commits=$(git log --all --oneline --no-merges | grep -v "bot" | wc -l)
echo "📊 總計: $total_devs 位開發者，共 $total_commits 次提交（不含 merge）"
echo ""
