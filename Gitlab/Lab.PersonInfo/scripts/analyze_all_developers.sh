#!/bin/bash

# 批次分析所有開發者
# 用法: ./analyze_all_developers.sh [start_date] [end_date]

START_DATE="${1:-2024-01-01}"
END_DATE="${2:-2024-12-31}"
REPO_ROOT="/mnt/d/lab/sample.dotblog"
OUTPUT_DIR="./output"

cd "$REPO_ROOT" || exit 1

echo "========================================"
echo "批次開發者分析工具"
echo "========================================"
echo "時間範圍: $START_DATE 到 $END_DATE"
echo ""

# 獲取所有開發者清單（排除 bot）
echo "正在獲取開發者清單..."
authors=$(git log --all --format="%an" --since="$START_DATE" --until="$END_DATE" | \
  sort -u | \
  grep -v "bot\|dependabot\|renovate")

author_count=$(echo "$authors" | wc -l)
echo "找到 $author_count 位開發者"
echo ""

# 創建匯總報告
SUMMARY_FILE="${OUTPUT_DIR}/summary_$(date +%Y%m%d_%H%M%S).md"

cat > "$SUMMARY_FILE" << EOF
# 團隊開發者技術評估匯總報告

**評估期間：** $START_DATE ~ $END_DATE  
**報告生成時間：** $(date '+%Y-%m-%d %H:%M:%S')  
**開發者數量：** $author_count 位

---

## 📊 整體統計

| 開發者 | 提交次數 | 程式碼變更 | 主要技術 | 評分 | 等級 |
|--------|----------|------------|----------|------|------|
EOF

current=0

# 逐一分析每位開發者
for author in $authors; do
  current=$((current + 1))
  echo "[$current/$author_count] 分析: $author"
  
  # 切換到工具目錄執行 Python 腳本
  cd /mnt/d/lab/sample.dotblog/Gitlab/Lab.PersonInfo || exit 1
  
  # 執行分析（靜默模式）
  python3 developer_analyzer.py "$author" "$START_DATE" "$END_DATE" > /dev/null 2>&1
  
  # 提取關鍵指標
  cd "$REPO_ROOT" || exit 1
  
  commits=$(git log --author="$author" --since="$START_DATE" --until="$END_DATE" --oneline | wc -l)
  
  stats=$(git log --author="$author" --since="$START_DATE" --until="$END_DATE" --pretty=tformat: --numstat | \
    awk '{ add += $1; subs += $2 } END { printf "+%s/-%s", add, subs }')
  
  # 主要技術（最常用的檔案類型）
  main_tech=$(git log --author="$author" --since="$START_DATE" --until="$END_DATE" --name-only --pretty=format: | \
    grep -o '\.[^.]*$' | sort | uniq -c | sort -rn | head -1 | awk '{print $2}' | sed 's/\.//')
  
  # 簡易評分（基於提交數）
  if [ "$commits" -gt 200 ]; then
    score="8-10"
    level="🏆 高級"
  elif [ "$commits" -gt 100 ]; then
    score="6-8"
    level="⭐ 中級"
  elif [ "$commits" -gt 50 ]; then
    score="4-6"
    level="📚 中級"
  else
    score="2-4"
    level="🌱 初級"
  fi
  
  # 寫入匯總表格
  echo "| $author | $commits | $stats | .$main_tech | $score | $level |" >> "$SUMMARY_FILE"
done

cd /mnt/d/lab/sample.dotblog/Gitlab/Lab.PersonInfo || exit 1

# 完成匯總報告
cat >> "$SUMMARY_FILE" << EOF

---

## 📈 分析說明

### 評分標準
- **8-10 分 (🏆 高級):** 提交 200+ 次，程式碼品質高，技術廣度優秀
- **6-8 分 (⭐ 中級):** 提交 100-200 次，貢獻穩定，具備專業能力  
- **4-6 分 (📚 中級):** 提交 50-100 次，正在成長中
- **2-4 分 (🌱 初級):** 提交 <50 次，參與度較低或剛加入

### 詳細報告
每位開發者的完整評估報告請查看 \`output/\` 目錄下的個別檔案。

---

**工具版本：** v1.0  
**數據來源：** Git Repository
EOF

echo ""
echo "========================================"
echo "✅ 分析完成！"
echo "========================================"
echo "個別報告: $OUTPUT_DIR/*.md"
echo "匯總報告: $SUMMARY_FILE"
echo ""
echo "報告數量:"
ls -1 "$OUTPUT_DIR"/*.md 2>/dev/null | wc -l
