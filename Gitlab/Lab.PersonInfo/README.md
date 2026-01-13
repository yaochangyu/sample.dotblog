# Git 開發者技術水平分析工具使用指南

## 📦 工具清單

本專案提供以下分析工具：

### 1. `analyze_developer.sh` - Shell 快速分析腳本
- **用途**：快速統計開發者的基本指標
- **優點**：輕量、快速、無依賴
- **輸出**：終端機文字輸出

### 2. `developer_analyzer.py` - Python 完整分析工具
- **用途**：生成詳細的 Markdown 評估報告
- **優點**：深度分析、評分系統、視覺化數據
- **輸出**：Markdown 格式報告

---

## 🚀 快速開始

### 步驟 0：列出所有開發者（必要步驟）

```bash
# 查看儲存庫中的所有開發者
./list_developers.sh
```

**輸出包含：**
- 📊 依提交次數排序的開發者列表
- 📧 開發者名稱與 Email 對照
- 📅 詳細統計（含最後提交時間）
- 📝 純名稱列表（適合複製使用）

**快速指令：**
```bash
# 只看名稱和提交次數
git shortlog -sn --all --no-merges

# 只看名稱（適合複製）
git log --all --format="%an" | sort -u
```

---

### 方法一：使用 Shell 腳本（簡單快速）

```bash
# 基本用法
./analyze_developer.sh "開發者名稱" "2024-01-01" "2024-12-31"

# 範例
./analyze_developer.sh "余小章" "2024-01-01" "2024-12-31"

# 輸出到檔案
./analyze_developer.sh "余小章" "2024-01-01" "2024-12-31" > output/report.txt
```

### 方法二：使用 Python 工具（完整報告）

```bash
# 基本用法
python3 developer_analyzer.py "開發者名稱" "起始日期" "結束日期"

# 範例
python3 developer_analyzer.py "余小章" "2024-01-01" "2024-12-31"

# 自動生成報告到 output/ 目錄
```

---

## 📊 分析所有開發者

### 批次分析腳本

創建 `analyze_all.sh`：

```bash
#!/bin/bash

# 獲取所有開發者清單
authors=$(cd /mnt/d/lab/sample.dotblog && git log --all --format="%an" | sort -u | grep -v "dependabot")

echo "開始批次分析..."

# 逐一分析
for author in $authors; do
    echo "分析: $author"
    python3 developer_analyzer.py "$author" "2024-01-01" "2024-12-31"
done

echo "✅ 全部完成！報告位於 output/ 目錄"
```

---

## 🔍 如何從版控評估技術水平？

### 核心評估維度

#### 1️⃣ 程式碼貢獻量 (15% 權重)
**看什麼：**
- 提交次數
- 程式碼新增/刪除行數
- 活躍天數
- 涉及檔案數

**怎麼判斷：**
- ✅ 好：穩定貢獻、涉及多個模組
- ⚠️ 注意：提交過少（參與度低）或過多（可能碎片化）

---

#### 2️⃣ Commit 品質 (25% 權重) ⭐ 最重要

**A. Message 規範**
```bash
# 優質範例
feat(auth): add JWT token validation
fix(api): resolve null pointer in user service
docs: update API documentation

# 劣質範例
update
fix bug
修改
```

**評估標準：**
- 遵循 Conventional Commits 格式 (feat/fix/docs/refactor)
- 長度適中 (50-72 字元)
- 描述具體清晰

**B. 變更粒度**
| 規模 | 理想佔比 | 說明 |
|------|----------|------|
| 小型 (≤100行) | 60%+ | 表示模組化思維好 |
| 中型 (100-500行) | 30% | 正常功能開發 |
| 大型 (>500行) | <10% | 過多表示缺乏拆分能力 |

**C. 修復率**
- <15%：優秀 ✅
- 15-30%：正常
- \>30%：可能程式碼品質問題 ⚠️

---

#### 3️⃣ 技術廣度 (20% 權重)

**看檔案類型分佈：**

**前端開發者：**
```
.js, .ts, .jsx, .tsx, .vue  → JavaScript/TypeScript
.css, .scss, .sass          → 樣式
.html, .cshtml              → 模板
```

**後端開發者：**
```
.cs                         → C# .NET
.java                       → Java
.py                         → Python
.go                         → Golang
```

**全棧開發者：**
- 前後端檔案都有涉及
- 可能包含 API 設計、資料庫遷移

**DevOps/SRE：**
```
Dockerfile                  → 容器化
.yml, .yaml                 → CI/CD 配置
.sh                         → 自動化腳本
.tf                         → Terraform (IaC)
```

**評分標準：**
- 1-2 種語言：專精型 (6分)
- 3-5 種：全棧 (8分)
- 5+ 種：技術廣度優秀 (10分)

---

#### 4️⃣ 協作能力 (15% 權重)

**指標：**
- Merge commits 數量
- Conflict 處理頻率
- 被 revert 的次數

**評估：**
```bash
# 查看 merge commits
git log --author="NAME" --merges

# 被 revert 的提交
git log --all --grep="Revert" | grep "NAME"
```

- Revert 率 <2%：優秀 ✅
- Revert 率 >5%：需改進 ⚠️

---

#### 5️⃣ 工作模式 (10% 權重)

**時間分佈分析：**

**優質模式：**
- 工作日集中（週一到週五）
- 工作時段 (9-18點) 提交率 >60%
- 每天穩定多次小 commit

**警訊：**
- 深夜/凌晨頻繁提交 → 可能時間管理問題
- 週末集中爆量 → 可能趕工
- 不規律提交 → 可能 deadline 驅動

---

#### 6️⃣ 進步趨勢 (15% 權重)

**對比早期 vs 近期：**

```bash
# 早期（前 6 個月）
git log --since="2024-01-01" --until="2024-06-30"

# 近期（最近 6 個月）
git log --since="2024-07-01" --until="2024-12-31"
```

**進步指標：**
- ✅ Commit message 品質提升
- ✅ 單次變更規模更合理
- ✅ 技術棧擴展
- ✅ 修復率降低

---

## 🎯 綜合評分模型

### 分級標準

| 等級 | 分數 | 特徵 |
|------|------|------|
| 🏆 **高級工程師** | 8-10 | Message 規範 90%+、變更粒度優、3+ 技術棧、修復率 <15%、有架構級變更 |
| ⭐ **中級工程師** | 5-7 | Message 規範 60-90%、變更粒度合理、2-3 技術棧、修復率 15-30% |
| 🌱 **初級工程師** | 1-4 | Message 不規範、大量修復、單一技術棧、變更集中小範圍 |

---

## 📋 實際案例解讀

### 案例：余小章 (評分 7.0/10 - 中級工程師)

**優勢：**
✅ **程式碼貢獻量 10/10**
- 202 次提交，+382K/-76K 行
- 涉及 1970 個檔案
- 表示：高度活躍、貢獻量大

✅ **技術廣度 10/10**
- 53 種檔案類型
- 主要：C# (47%) + Frontend (10%)
- 表示：全棧能力、技術多樣

**需改進：**
⚠️ **Commit 品質 3.8/10**
- Conventional Commits 僅 1%
- 大型 commit 佔比 27.5%
- 建議：
  1. 採用規範的 commit message
  2. 將大型變更拆分為小 commit

⚠️ **工作模式 4.9/10**
- 工作時段提交率僅 49%
- 週末/晚上提交較多
- 建議：優化工作時間分配

---

## 🛠️ 進階分析技巧

### 1. 查看特定開發者的程式碼熱區

```bash
# 最常修改的檔案
git log --author="NAME" --name-only --pretty=format: | 
  sort | uniq -c | sort -rn | head -20
```

### 2. 分析程式碼品質趨勢

```bash
# 按月統計提交數
git log --author="NAME" --date=format:"%Y-%m" --pretty=format:"%ad" | 
  sort | uniq -c
```

### 3. 找出最大的單次變更

```bash
git log --author="NAME" --shortstat --oneline | 
  grep -B1 "files changed" | head -20
```

### 4. 技術棧雷達圖數據

```bash
git log --author="NAME" --name-only --pretty=format: | 
  awk -F. '{if(NF>1) print $NF}' | 
  sort | uniq -c | sort -rn
```

---

## ⚠️ 分析局限性

### 無法評估的能力：
1. **程式碼邏輯品質** - 需 Code Review
2. **演算法效率** - 需效能測試
3. **安全意識** - 需安全掃描
4. **架構設計能力** - 需設計文檔
5. **溝通協作** - 需 PR 討論記錄

### 可能失真的情況：
- 使用 squash merge 會隱藏實際提交數
- Pair programming 提交歸屬不明確
- 接手遺留專案會有大量刪除
- 使用生成工具會灌水

---

## 💡 實用建議

### 對管理者：
1. **不要單純看提交數量** - 質量比數量重要
2. **結合人工 Code Review** - Git 數據僅供參考
3. **觀察進步趨勢** - 成長比當前狀態重要
4. **定期評估** - 建議季度或半年評估一次

### 對開發者：
1. **重視 Commit Message** - 這是最容易改進的
2. **學會拆分 Commit** - 一個 commit 只做一件事
3. **減少修復性提交** - 加強測試覆蓋
4. **擴展技術棧** - 但要有深度
5. **規律工作** - 避免 deadline 驅動

---

## 📞 常見問題

**Q: 為什麼我的評分這麼低？**
A: 最常見原因是 Commit Message 不規範（佔 25% 權重）。建議立即採用 Conventional Commits。

**Q: 提交次數少會影響評分嗎？**
A: 會，但影響不大（僅 15%）。品質比數量重要。

**Q: 如何快速提升評分？**
A: 
1. 規範 commit message（立即見效）
2. 拆分大型 commit（中期見效）
3. 減少 bug 修復比例（長期見效）

**Q: 全棧和專精哪個評分高？**
A: 各有優勢。全棧在「技術廣度」得分高，專精則需在「程式碼品質」補分。

---

## 📚 參考資源

- [Conventional Commits](https://www.conventionalcommits.org/)
- [如何寫好 Git Commit Message](https://chris.beams.io/posts/git-commit/)
- [Git 最佳實踐](https://sethrobertson.github.io/GitBestPractices/)

---

**工具版本：** v1.0  
**最後更新：** 2026-01-13  
**維護者：** Lab.PersonInfo Team
