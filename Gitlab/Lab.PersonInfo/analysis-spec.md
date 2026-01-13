# 🎯 從版控系統評估開發者技術水平完整指南

## 📚 核心問題解答

### ❓ 如何從版控知道開發者的技術水平？

從 Git 版控系統可以透過以下 **6 大維度** 客觀評估開發者技術能力：

---

## 1️⃣ 程式碼貢獻量 (15% 權重)

### 看什麼：
- ✅ 提交次數（活躍度）
- ✅ 程式碼新增/刪除行數（產出量）
- ✅ 活躍天數（持續性）
- ✅ 涉及檔案數（廣度）

### 怎麼判斷：
```bash
# 查看基礎統計
git log --author="開發者名稱" --oneline | wc -l  # 提交次數
git log --author="開發者名稱" --shortstat        # 變更統計
```

**評分標準：**
- 🏆 200+ 次提交：高活躍度 (10分)
- ⭐ 100-200 次：穩定貢獻 (8分)
- 📚 50-100 次：中等參與 (6分)
- 🌱 <50 次：參與度低 (4分)

**⚠️ 注意陷阱：**
- 提交過多可能是拆分過細或頻繁修 bug
- 需排除自動生成的檔案（package-lock.json, dist/）

---

## 2️⃣ Commit 品質 (25% 權重) ⭐ **最重要**

### A. Message 規範性

**優質範例：**
```
feat(auth): add JWT token validation
fix(api): resolve null pointer in user service
docs(readme): update installation guide
refactor(utils): extract common validation logic
```

**劣質範例：**
```
update
fix bug
修改
WIP
```

**檢查方法：**
```bash
# 查看所有 commit messages
git log --author="開發者" --pretty=format:"%s"

# 統計符合 Conventional Commits 的比例
git log --author="開發者" --pretty=format:"%s" | \
  grep -E "^(feat|fix|docs|refactor|test|chore|style|perf)(\(.+\))?:" | \
  wc -l
```

**評分標準：**
- ✅ 規範率 >80%：優秀 (9-10分)
- ⚠️ 規範率 40-80%：中等 (5-8分)
- ❌ 規範率 <40%：需改進 (1-4分)

---

### B. 變更粒度（單次 commit 大小）

**黃金比例：**
| 規模 | 行數 | 理想佔比 | 說明 |
|------|------|----------|------|
| 小型 | ≤100 行 | **60%+** | 模組化思維好 ✅ |
| 中型 | 100-500 行 | 30% | 正常功能開發 |
| 大型 | >500 行 | <10% | 過多表示缺乏拆分能力 ⚠️ |

**檢查方法：**
```bash
# 查看每次變更的規模
git log --author="開發者" --shortstat --oneline
```

**評分邏輯：**
- 小型變更佔比 >60%：優秀 (9-10分)
- 小型變更佔比 40-60%：良好 (6-8分)
- 小型變更佔比 <40%：需改進 (1-5分)

---

### C. 修復性提交比例

**檢查方法：**
```bash
# 統計包含修復關鍵字的提交
git log --author="開發者" --grep="fix\|bug\|hotfix\|revert" --oneline | wc -l
```

**評分標準：**
- ✅ 修復率 <15%：程式碼品質高
- ⚠️ 修復率 15-30%：正常範圍
- ❌ 修復率 >30%：品質問題或測試不足

---

## 3️⃣ 技術廣度與深度 (20% 權重)

### 看檔案類型分佈

**前端開發者：**
```
.js, .ts, .jsx, .tsx     → React/Vue/Angular
.css, .scss, .sass       → 樣式設計
.html, .cshtml           → 模板
```

**後端開發者：**
```
.cs                      → C# .NET
.java                    → Java Spring
.py                      → Python Django/Flask
.go                      → Golang
```

**全棧開發者：**
- 前後端檔案都有涉及
- 包含 API、資料庫、前端 UI

**DevOps/SRE：**
```
Dockerfile               → 容器化
.yml, .yaml              → CI/CD
.sh, .bash               → 自動化腳本
.tf                      → Terraform (基礎設施即程式碼)
```

**檢查方法：**
```bash
# 統計檔案類型分佈
git log --author="開發者" --name-only --pretty=format: | \
  grep -v '^$' | \
  sed 's/.*\(\.[^.]*\)$/\1/' | \
  sort | uniq -c | sort -rn
```

**評分標準：**
- 🏆 5+ 種語言：技術廣度優秀 (10分)
- ⭐ 3-5 種：全棧能力 (8分)
- 📚 1-2 種：專精型 (6分)

---

## 4️⃣ 協作能力 (15% 權重)

### A. Merge Commits（協作參與度）

```bash
# 查看 merge commits
git log --author="開發者" --merges --oneline
```

- 有 merge commits：表示參與分支協作
- 無 merge commits：可能獨立開發或使用 rebase

### B. 被 Revert 的次數（程式碼穩定性）

```bash
# 查找被回退的提交
git log --all --grep="Revert" | grep "開發者名稱"
```

**評分標準：**
- ✅ Revert 率 <2%：優秀
- ⚠️ Revert 率 2-5%：正常
- ❌ Revert 率 >5%：需改進

### C. Conflict 處理（衝突解決能力）

```bash
# 查找包含 conflict 的提交
git log --all --grep="conflict\|merge" --oneline
```

---

## 5️⃣ Code Review 品質 (10% 權重) ⭐ **新增維度**

### 為什麼 Code Review 很重要？

Code Review 是評估開發者技術水平的關鍵指標：
- ✅ **技術深度**：能否發現架構問題、效能問題、安全漏洞
- ✅ **協作態度**：是否認真 Review 還是直接 Approve
- ✅ **學習能力**：是否從別人的程式碼中學習
- ✅ **影響力**：資深開發者應積極參與 Review，提升團隊水平

⚠️ **重要提醒**：Code Review 資料無法從純 Git 命令取得，需要整合 GitHub/GitLab API。

---

### A. Review 參與度 (30%)

**評估指標**：
```bash
# 需使用 GitHub/GitLab API
# 統計開發者作為 Reviewer 的 PR/MR 數量
# 統計 Review Comments 總數
```

**GitHub GraphQL 查詢範例**：
```graphql
query GetDeveloperReviews($username: String!, $from: DateTime!) {
  user(login: $username) {
    contributionsCollection(from: $from) {
      pullRequestReviewContributions(first: 100) {
        totalCount
        nodes {
          pullRequestReview {
            state
            comments { totalCount }
            createdAt
          }
        }
      }
    }
  }
}
```

**GitLab API 範例**：
```bash
# 取得開發者的 MR Review 活動
curl --header "PRIVATE-TOKEN: <your_token>" \
  "https://gitlab.com/api/v4/projects/:id/merge_requests?reviewer_id=:user_id&created_after=2024-01-01"
```

**評分標準**：
- 🏆 Review 數量 > 團隊平均 1.5 倍：優秀 (9-10分)
- ⭐ Review 數量 = 團隊平均：良好 (7-8分)
- 📚 Review 數量 = 團隊平均 50-100%：中等 (5-6分)
- 🌱 Review 數量 < 團隊平均 50%：需改進 (1-4分)

---

### B. Review 深度 (40%) ⭐ **最重要**

#### 1. 有建議的 Review 比例

**優質 Review Comment 範例**：
```markdown
這裡的 SQL 查詢可能有 N+1 問題，建議改用 JOIN：

// 目前寫法（會產生 100 次查詢）
const users = await User.findAll();
for (const user of users) {
  user.posts = await Post.findAll({ where: { userId: user.id } });
}

// 建議修改（只需 1 次查詢）
const users = await User.findAll({
  include: [{ model: Post }]
});

這樣可以將查詢次數從 100 次減少到 1 次，大幅提升效能。
```

**劣質 Review Comment 範例**：
```markdown
LGTM
看起來不錯
👍
沒問題
```

**評分標準**：
- ✅ 80%+ 的 Review 包含具體建議：優秀 (9-10分)
- ⚠️ 50-80% 有建議：中等 (5-8分)
- ❌ <50% 只是 LGTM/Approve：需改進 (1-4分)

---

#### 2. 發現問題的嚴重等級

**問題分類**：
```
Critical Issues（關鍵問題）：
- 安全漏洞（SQL Injection, XSS）
- 資料遺失風險
- 系統穩定性問題
→ 每發現一個 +5 分

Major Issues（重要問題）：
- 效能問題（N+1 查詢、記憶體洩漏）
- 邏輯錯誤
- 架構設計問題
→ 每發現一個 +3 分

Minor Issues（小問題）：
- 程式碼風格不一致
- 命名不清楚
- 缺少註解
→ 每發現一個 +1 分
```

**評分邏輯**：
```
Review 深度分數 = min(10, 問題總分 / Review 次數)
```

---

### C. Review 時效性 (20%)

**評估指標**：
```
平均 Review 回應時間 = Σ(首次 Review 時間 - PR 建立時間) / PR 總數
```

**為什麼時效性重要**：
- ⏱️ Review 太慢會阻塞開發流程
- ⏱️ 提交者需要等待 Review 才能 merge
- ⏱️ PR 開太久容易產生 conflict

**評分標準**：
- ⚡ < 4 小時：優秀 (9-10分)
- ⭐ 4-24 小時：良好 (7-8分)
- 📚 24-72 小時：普通 (5-6分)
- 🐌 > 72 小時：阻礙開發流程 (1-4分)

---

### D. 被 Review 的接受度 (10%)

**評估指標**：
```bash
# 統計被 Request Changes 的比例
被 Request Changes 率 = Request Changes 次數 / 提交的 PR 總數

# 統計需要二次 Review 的比例
二次 Review 率 = 需要 Re-review 的 PR / 總 PR 數
```

**評分標準**：
- ✅ Request Changes 率 < 15%：程式碼品質高 (9-10分)
- ⚠️ Request Changes 率 15-30%：正常範圍 (7-8分)
- ❌ Request Changes 率 > 30%：品質需改進 (1-6分)

**⚠️ 注意**：低 Request Changes 率不一定是好事，也可能表示：
- Reviewer 不夠認真
- 團隊 Review 文化不佳
- 開發者只挑簡單的 PR Review

---

### 📊 Code Review 實戰案例

#### 案例 1：表面參與型 Reviewer

**數據**：
- Review 次數：80 次/半年 ✅（看似很高）
- 平均每個 Review 的 Comments：0.8 個 ❌
- LGTM-only 比例：75% ❌

**問題診斷**：
- 只是點 Approve，沒有實質審查
- 無法發揮 Code Review 的價值

**改進建議**：
- 每個 Review 至少留 2-3 條有價值的建議
- 關注程式碼邏輯、效能、安全性
- 學習如何給出建設性回饋

---

#### 案例 2：高品質 Reviewer

**數據**：
- Review 次數：45 次/半年
- 平均每個 Review 的 Comments：4.2 個 ✅
- 發現 Critical Issues：8 個 ✅
- 發現 Major Issues：23 個 ✅
- 平均回應時間：2.3 小時 ✅

**評價**：
- Review 數量適中但品質極高
- 能發現真正的問題而非雞毛蒜皮
- 回應及時不阻塞開發流程

---

## 6️⃣ 工作模式與效率 (10% 權重)

### 時間分佈分析

**查看星期分佈：**
```bash
git log --author="開發者" --date=format:"%A" --pretty=format:"%ad" | 
  sort | uniq -c
```

**查看小時分佈：**
```bash
git log --author="開發者" --date=format:"%H" --pretty=format:"%ad" | 
  sort | uniq -c
```

**優質工作模式：**
- ✅ 工作日集中（週一到週五）
- ✅ 工作時段 (9-18點) 提交率 >60%
- ✅ 每天穩定多次小 commit

**警訊模式：**
- ⚠️ 深夜/凌晨頻繁提交 → 時間管理問題
- ⚠️ 週末集中爆量 → 可能趕工
- ⚠️ 不規律提交 → deadline 驅動

---

## 7️⃣ 進步趨勢 (15% 權重)

### 時間切片對比

```bash
# 早期表現（前 6 個月）
git log --author="開發者" --since="2024-01-01" --until="2024-06-30"

# 近期表現（最近 6 個月）
git log --author="開發者" --since="2024-07-01" --until="2024-12-31"
```

**進步指標：**
- ✅ Commit message 品質提升
- ✅ 單次變更規模更合理
- ✅ 技術棧擴展
- ✅ 修復率降低

**使用本工具：**
```bash
python3 progress_analyzer.py "開發者" "2024-01-01" "2024-06-30" "2024-07-01" "2024-12-31"
```

---

## 🎯 綜合評分公式

### 權重分配（已更新）

| 維度 | 權重 | 關鍵指標 | 變更說明 |
|------|------|----------|---------|
| 程式碼貢獻量 | 12% | 提交次數、活躍度 | ↓ -3% |
| **Commit 品質** | **23%** | Message 規範、變更粒度、修復率 | ↓ -2% |
| 技術廣度 | 18% | 語言種類、技術棧覆蓋 | ↓ -2% |
| 協作能力 | 12% | Merge Commits、衝突處理、Revert 率 | ↓ -3% |
| **Code Review 品質** | **10%** | **Review 參與度、深度、時效性** | **🆕 新增** |
| 工作模式 | 10% | 時間分佈、穩定性 | 不變 |
| 進步趨勢 | 15% | 成長曲線、技能提升 | 不變 |

### 總分計算

```
總分 = (貢獻量得分 × 0.12) +
       (品質得分 × 0.23) +
       (技術廣度得分 × 0.18) +
       (協作得分 × 0.12) +
       (Code Review 得分 × 0.10) +
       (工作模式得分 × 0.10) +
       (進步趨勢得分 × 0.15)
```

**⚠️ 權重調整說明**：
- 新增 Code Review 品質維度（10%），從其他維度調整而來
- 降低單純的「數量」權重（貢獻量 -3%），提升「品質」權重
- 協作能力與 Code Review 分開評估，更精確

---

## 📊 分級標準（已更新）

| 等級 | 分數 | 特徵描述 |
|------|------|----------|
| 🏆 **高級工程師** | 8-10 | • Message 規範率 90%+<br>• 小型變更佔比 80%+<br>• 涉及 3+ 技術棧<br>• 修復率 <15%<br>• 有架構級別變更<br>• **Review 參與度高，能發現 Critical Issues** ⭐ |
| ⭐ **中級工程師** | 5-7 | • Message 規範率 60-90%<br>• 變更粒度合理<br>• 2-3 種技術棧<br>• 修復率 15-30%<br>• 功能開發為主<br>• **有 Code Review 參與但深度一般** |
| 🌱 **初級工程師** | 1-4 | • Message 不規範<br>• 大量修復性提交<br>• 單一技術棧<br>• 變更集中於小範圍<br>• **Code Review 參與少或僅 LGTM** |

---

## ❓ 如何從版控知道程式碼品質？

### 直接指標（可從 Git 獲得）

#### 1. Commit Message 品質
- **規範性**：是否遵循 Conventional Commits
- **描述性**：是否清楚說明變更內容
- **長度**：50-72 字元為佳

#### 2. 變更粒度
- **單一職責**：一個 commit 只做一件事
- **原子性**：commit 應該是可獨立測試的最小單位
- **大小適中**：避免 500+ 行的巨型提交

#### 3. 修復率
- **低修復率** (<15%)：表示前期開發品質高
- **高修復率** (>30%)：可能缺乏測試或設計不周

#### 4. Revert 頻率
- **被 revert 次數**：越少越好
- **自己 revert 自己**：表示提交前缺乏驗證

---

### 間接指標（需結合其他工具）

#### 5. 測試覆蓋率變化
```bash
# 查看測試檔案變化
git log --author="開發者" -- "**/*test*" --oneline | wc -l
```

- 測試檔案增加 → 品質意識高
- 只改業務程式碼 → 可能忽略測試

#### 6. 程式碼審查參與度
```bash
# 需要 GitHub/GitLab API
# 查看 PR/MR comments、approve 記錄
```

#### 7. 重構行為
```bash
# 查找重構相關提交
git log --author="開發者" --grep="refactor" --oneline
```

- 主動重構 → 程式碼品質意識強
- 從不重構 → 可能技術債累積

---

## 🛠️ 實用工具使用

### 1. 快速分析（Shell 腳本）
```bash
./analyze_developer.sh "開發者名稱" "2024-01-01" "2024-12-31"
```

### 2. 完整報告（Python 工具）
```bash
python3 developer_analyzer.py "開發者名稱" "2024-01-01" "2024-12-31"
# 自動生成 Markdown 報告到 output/ 目錄
```

### 3. 批次分析所有開發者
```bash
./analyze_all_developers.sh "2024-01-01" "2024-12-31"
# 生成團隊匯總報告
```

### 4. 進步趨勢分析
```bash
python3 progress_analyzer.py "開發者" "2024-01-01" "2024-06-30" "2024-07-01" "2024-12-31"
# 對比兩個時間段的成長
```

---

## ⚠️ 評估局限性

### 無法評估的能力：

1. **程式碼邏輯品質**
   - 需要：Code Review、靜態分析工具（SonarQube）
   - Git 無法反映：演算法效率、邏輯正確性

2. **安全意識**
   - 需要：安全掃描工具（Snyk, Dependabot）
   - Git 無法反映：SQL Injection、XSS 防護

3. **架構設計能力**
   - 需要：設計文檔、架構圖
   - Git 只能看到檔案層級，不知設計思路

4. **溝通協作**
   - 需要：PR 討論、會議記錄
   - Git 只有 commit，無法看到協作過程

5. **業務理解**
   - 需要：需求文檔、業務討論
   - Git 無法反映業務價值判斷

---

### 可能失真的情況：

1. **Squash Merge**
   - 問題：多個 commit 合併成一個
   - 影響：隱藏實際提交數和變更歷史

2. **Pair Programming**
   - 問題：兩人協作但 commit 歸一人
   - 影響：貢獻度統計失真

3. **接手遺留專案**
   - 問題：大量刪除舊程式碼
   - 影響：刪除/新增比例異常

4. **使用生成工具**
   - 問題：Scaffold、模板生成大量程式碼
   - 影響：程式碼量灌水

---

## 💡 最佳實踐建議

### 對管理者：

✅ **不要單純看數量**
- 質量比數量重要
- 200 次垃圾提交 < 50 次高品質提交

✅ **結合多種評估方式**
- Git 數據 + Code Review
- 靜態分析 + 效能測試
- 同儕評價 + 業務成果

✅ **觀察進步趨勢**
- 成長比當前狀態重要
- 定期（季度/半年）評估

✅ **避免過度量化**
- 數據僅供參考，不是 KPI
- 不要讓開發者為了指標而優化指標

---

### 對開發者：

🎯 **立即可改進（1 週內）**
1. ✅ 採用 Conventional Commits 格式
2. ✅ 寫清楚 commit message（說明為什麼，不只是什麼）
3. ✅ 提交前用 `git diff` 檢查變更

🎯 **中期改進（1-3 個月）**
1. ✅ 學會拆分 commit（一個 commit 一件事）
2. ✅ 增加測試覆蓋率
3. ✅ 定期重構技術債

🎯 **長期提升（6 個月+）**
1. ✅ 擴展技術棧（但保持深度）
2. ✅ 參與架構設計
3. ✅ 主動 Code Review 他人程式碼

---

## 📚 延伸閱讀

- [Conventional Commits 規範](https://www.conventionalcommits.org/)
- [如何寫好 Git Commit Message](https://chris.beams.io/posts/git-commit/)
- [Git 最佳實踐](https://sethrobertson.github.io/GitBestPractices/)
- [Google Engineering Practices](https://google.github.io/eng-practices/)

---

## 🎓 總結

從版控系統評估開發者技術水平是一種 **客觀但不完整** 的方法：

✅ **優勢：**
- 數據客觀、可量化
- 可追蹤長期趨勢
- 自動化分析、低成本

⚠️ **限制：**
- 無法評估邏輯品質
- 無法反映業務價值
- 可能被指標操控

💡 **最佳應用：**
將 Git 分析作為 **初步篩選** 和 **趨勢觀察** 工具，結合 Code Review、業務成果、同儕評價等多維度評估，才能全面了解開發者的真實技術水平。

---

**文件版本：** v2.0
**最後更新：** 2026-01-13
**作者：** Lab.PersonInfo Team
**主要變更：** 新增「Code Review 品質」評估維度（10% 權重），調整評分權重配置，補充 GitHub/GitLab API 整合範例
