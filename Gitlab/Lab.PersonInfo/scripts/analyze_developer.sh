#!/bin/bash

# 開發者技術水平分析腳本
# 用法: ./analyze_developer.sh [author_name_or_email] [start_date] [end_date]

AUTHOR="${1:-yaochangyu}"
START_DATE="${2:-2020-01-01}"
END_DATE="${3:-2026-12-31}"
OUTPUT_DIR="./output"
REPO_ROOT="/mnt/d/lab/sample.dotblog"

cd "$REPO_ROOT" || exit 1

echo "=== 分析開發者: $AUTHOR ==="
echo "=== 時間範圍: $START_DATE 到 $END_DATE ==="
echo ""

# 1. 基礎統計
echo "## 1. 基礎統計數據"
echo "總提交次數:"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --oneline | wc -l

echo ""
echo "程式碼變更統計 (新增/刪除):"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --pretty=tformat: --numstat | \
  awk '{ add += $1; subs += $2; loc += $1 - $2 } END { printf "新增: %s 行\n刪除: %s 行\n淨變更: %s 行\n", add, subs, loc }'

echo ""
echo "活躍天數:"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --date=short --format="%ad" | sort -u | wc -l

echo ""
echo "涉及檔案數:"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --name-only --pretty=format: | sort -u | grep -v '^$' | wc -l

# 2. Commit 品質分析
echo ""
echo "## 2. Commit 品質指標"
echo "平均每次 commit 變更行數:"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --shortstat | \
  grep -E "files? changed" | \
  awk '{files+=$1; inserted+=$4; deleted+=$6; count++} END {if(count>0) print int((inserted+deleted)/count) " 行/commit"}'

echo ""
echo "Commit Message 長度統計 (平均字元數):"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --pretty=format:"%s" | \
  awk '{sum+=length; count++} END {if(count>0) print int(sum/count) " 字元"}'

echo ""
echo "修復性 Commit 比例:"
total_commits=$(git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --oneline | wc -l)
fix_commits=$(git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --oneline | grep -iE "(fix|bug|hotfix|revert|修復)" | wc -l)
if [ "$total_commits" -gt 0 ]; then
  echo "修復相關: $fix_commits / $total_commits ($(awk "BEGIN {printf \"%.1f\", ($fix_commits/$total_commits)*100}")%)"
fi

# 3. 技術棧分析
echo ""
echo "## 3. 技術棧分析"
echo "檔案類型分佈 (Top 10):"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --name-only --pretty=format: | \
  grep -v '^$' | \
  sed 's/.*\.//' | \
  sort | uniq -c | sort -rn | head -10

echo ""
echo "主要工作目錄 (Top 10):"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --name-only --pretty=format: | \
  grep -v '^$' | \
  sed 's/\/[^\/]*$//' | \
  sort | uniq -c | sort -rn | head -10

# 4. 協作指標
echo ""
echo "## 4. 協作指標"
echo "Merge Commits:"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --merges --oneline | wc -l

echo ""
echo "被 Revert 的 Commits:"
git log --all --since="$START_DATE" --until="$END_DATE" --grep="Revert" --pretty=format:"%s" | \
  grep -i "$AUTHOR" | wc -l

# 5. 時間分析
echo ""
echo "## 5. 工作模式分析"
echo "每週提交分佈:"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --date=format:"%A" --pretty=format:"%ad" | \
  sort | uniq -c | sort -k2

echo ""
echo "每小時提交分佈:"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --date=format:"%H" --pretty=format:"%ad" | \
  sort | uniq -c | sort -k2 -n

# 6. 程式碼複雜度指標
echo ""
echo "## 6. 程式碼複雜度"
echo "單次最大變更 (Top 5):"
git log --author="$AUTHOR" --since="$START_DATE" --until="$END_DATE" --shortstat --oneline | \
  awk '/^ / {print prev " | " $0; prev=""} !/^ / {prev=$0}' | \
  awk -F'|' '{
    if ($2 ~ /[0-9]+ file/) {
      match($2, /([0-9]+) insertion/, ins);
      match($2, /([0-9]+) deletion/, del);
      total = ins[1] + del[1];
      print total " 行 | " $1
    }
  }' | sort -rn | head -5

echo ""
echo "=== 分析完成 ==="
