# GitLab 開發者分析系統實作計畫

## 📌 目標
建立一套完整的 GitLab 開發者技術水平分析系統，根據 `analysis-spec.md` 的評估維度，自動化收集、分析並產生開發者貢獻報告。

---

## 🏗️ 系統架構

```
Lab.PersonInfo/
├── scripts/                          # 所有腳本
│   ├── config/                       # 配置檔案
│   │   ├── gitlab_config.py          # GitLab 連線設定
│   │   └── analysis_config.py        # 分析參數設定
│   ├── collectors/                   # 數據收集器
│   │   ├── gitlab_api_collector.py   # GitLab API 數據收集
│   │   ├── git_local_collector.py    # Git 本地數據收集
│   │   └── data_merger.py            # 數據合併與清洗
│   ├── analyzers/                    # 分析器
│   │   ├── commit_analyzer.py        # Commit 品質分析
│   │   ├── review_analyzer.py        # Code Review 分析
│   │   ├── collaboration_analyzer.py # 協作能力分析
│   │   └── progress_analyzer.py      # 進步趨勢分析
│   ├── reporters/                    # 報告生成器
│   │   ├── markdown_reporter.py      # Markdown 報告
│   │   ├── csv_exporter.py           # CSV 導出
│   │   └── visualization.py          # 視覺化圖表
│   ├── output/                       # 輸出目錄
│   │   ├── raw/                      # 原始數據
│   │   ├── processed/                # 處理後數據
│   │   └── reports/                  # 最終報告
│   └── main.py                       # 主程式入口
└── analysis-spec.md                  # 評估規範文件
```

---

## 📝 實作步驟

### 🔹 階段一：環境準備與配置 (預估時間：完成後標記)

#### ☐ 步驟 1：建立專案目錄結構
**為什麼需要**：
- 清晰的目錄結構有助於程式碼管理與維護
- 分離數據收集、分析、報告生成的職責
- 方便後續擴展功能模組

**產出**：
- 完整的目錄結構
- `.gitignore` 檔案（排除敏感資訊和輸出檔案）

---

#### ☐ 步驟 2：建立 GitLab 連線配置
**為什麼需要**：
- GitLab API 需要 Personal Access Token 進行認證
- 需要配置 GitLab 實例 URL（可能是 gitlab.com 或私有部署）
- 將敏感資訊與程式碼分離（使用環境變數或配置檔）

**產出**：
- `scripts/config/gitlab_config.py`
- `.env.example` 範本檔案
- 配置載入與驗證機制

**GitLab API Token 所需權限**：
```
read_api          # 讀取 API 資源
read_repository   # 讀取 Repository
read_user         # 讀取用戶資訊
```

---

#### ☐ 步驟 3：建立分析參數配置
**為什麼需要**：
- 根據 `analysis-spec.md` 定義的權重比例設定參數
- 可調整的評分標準（如小型變更行數閾值）
- 時間範圍、專案過濾規則等配置

**產出**：
- `scripts/config/analysis_config.py`
- 包含所有評估維度的權重與閾值

---

### 🔹 階段二：數據收集模組 (預估時間：完成後標記)

#### ☐ 步驟 4：實作 GitLab API 數據收集器
**為什麼需要**：
- Git 本地命令無法取得 Code Review、MR、Issues 等數據
- 需要透過 GitLab REST API 或 GraphQL API 取得完整資訊
- 這是評估「Code Review 品質」(10% 權重) 的唯一數據來源

**收集的數據**：
```python
# 專案列表
- 所有可訪問的專案 ID、名稱、路徑

# Merge Request 數據
- MR 建立者、Reviewer、狀態、建立/合併時間
- MR Diff 統計（新增/刪除行數）
- MR Labels、Milestone

# Review Comments 數據
- Comment 類型（Diff Note, Discussion Note）
- Comment 內容、位置（檔案、行數）
- Comment 作者、時間
- 是否被標記為 Resolved

# Commit 數據（補充 Git 本地數據）
- Commit SHA、作者、時間、訊息
- 關聯的 MR ID
- CI/CD Pipeline 狀態

# Issues 數據（可選，用於評估協作能力）
- Issue 建立者、參與者、關閉者
- Issue 描述品質、標籤使用
```

**產出**：
- `scripts/collectors/gitlab_api_collector.py`
- 輸出：`scripts/output/raw/gitlab_projects.csv`
- 輸出：`scripts/output/raw/gitlab_merge_requests.csv`
- 輸出：`scripts/output/raw/gitlab_review_comments.csv`
- 輸出：`scripts/output/raw/gitlab_commits.csv`

**關鍵技術點**：
- GitLab API 分頁處理（每頁最多 100 筆）
- Rate Limiting 處理（避免 API 請求過快）
- 錯誤重試機制
- 增量更新邏輯（只抓取新數據）

---

#### ☐ 步驟 5：實作 Git 本地數據收集器
**為什麼需要**：
- 雖然 GitLab API 可以取得 Commit 數據，但本地 Git 命令更快速
- 可以取得更詳細的檔案變更統計
- 作為 API 數據的補充與驗證

**收集的數據**：
```bash
# 基於 analysis-spec.md 的命令
- git log --author --oneline --shortstat
- git log --pretty=format:"%s" --author
- git log --name-only --author
- git log --date=format:"%A" --pretty=format:"%ad"
```

**產出**：
- `scripts/collectors/git_local_collector.py`
- 輸出：`scripts/output/raw/git_commits.csv`
- 輸出：`scripts/output/raw/git_file_changes.csv`

---

#### ☐ 步驟 6：實作數據合併與清洗模組
**為什麼需要**：
- GitLab API 的開發者可能用 username，Git 本地用 email
- 需要建立「開發者統一身份映射表」
- 排除 bot 賬號（Renovate, Dependabot, GitLab Bot）
- 排除自動生成的檔案（package-lock.json, dist/, build/）

**處理邏輯**：
```python
# 身份統一
username <-> email <-> name 映射表

# 排除規則
- bot 賬號過濾
- 自動生成檔案過濾
- Merge commit 標記
```

**產出**：
- `scripts/collectors/data_merger.py`
- 輸出：`scripts/output/processed/unified_developers.csv`
- 輸出：`scripts/output/processed/all_commits_merged.csv`
- 輸出：`scripts/output/processed/all_reviews_merged.csv`

---

### 🔹 階段三：分析模組 (預估時間：完成後標記)

#### ☐ 步驟 7：實作 Commit 品質分析器 (23% 權重)
**為什麼需要**：
- 這是權重最高的評估維度之一
- 包含 Message 規範性、變更粒度、修復率三個子維度

**分析指標**：
```python
# A. Message 規範性
- Conventional Commits 符合率
- 評分：>80% 優秀 (9-10分), 40-80% 中等 (5-8分), <40% 需改進 (1-4分)

# B. 變更粒度
- 小型變更 (≤100行) 佔比
- 中型變更 (100-500行) 佔比
- 大型變更 (>500行) 佔比
- 評分：小型 >60% 優秀 (9-10分)

# C. 修復性提交比例
- 包含 fix|bug|hotfix|revert 的 commit 比例
- 評分：<15% 優秀, 15-30% 正常, >30% 需改進
```

**產出**：
- `scripts/analyzers/commit_analyzer.py`
- 輸出：`scripts/output/processed/commit_quality_scores.csv`

---

#### ☐ 步驟 8：實作 Code Review 品質分析器 (10% 權重)
**為什麼需要**：
- 這是新增的評估維度，完全依賴 GitLab API 數據
- 包含 Review 參與度、深度、時效性、被 Review 接受度

**分析指標**：
```python
# A. Review 參與度 (30%)
- Review 數量與團隊平均比較
- Review Comments 總數

# B. Review 深度 (40%) - 最重要
- 有建議的 Review 比例（排除 LGTM-only）
- 發現問題的嚴重等級統計
  - Critical Issues: SQL Injection, XSS (+5分/個)
  - Major Issues: N+1查詢, 記憶體洩漏 (+3分/個)
  - Minor Issues: 程式碼風格 (+1分/個)

# C. Review 時效性 (20%)
- 平均首次 Review 回應時間
- 評分：<4小時 優秀, 4-24小時 良好, >72小時 阻礙開發

# D. 被 Review 接受度 (10%)
- Request Changes 率
- 需要二次 Review 的比例
```

**產出**：
- `scripts/analyzers/review_analyzer.py`
- 輸出：`scripts/output/processed/review_quality_scores.csv`

**⚠️ 挑戰**：
- 如何自動判斷 Review Comment 是「有建議」還是「LGTM-only」？
  - 使用關鍵字匹配 + 長度判斷
  - 可選：整合 LLM API 進行語義分析
- 如何判斷問題嚴重等級？
  - 建立關鍵字規則庫（如 "SQL Injection" → Critical）

---

#### ☐ 步驟 9：實作協作能力分析器 (12% 權重)
**為什麼需要**：
- 評估開發者在團隊中的協作表現
- 包含 Merge Commits、被 Revert 次數、Conflict 處理

**分析指標**：
```python
# A. Merge Commits 參與度
- 有 Merge Commits 表示參與分支協作

# B. 被 Revert 的次數
- Revert 率：<2% 優秀, 2-5% 正常, >5% 需改進

# C. MR 建立與合併
- 建立的 MR 數量
- 被 Approve 的比例
- 平均 MR 存活時間
```

**產出**：
- `scripts/analyzers/collaboration_analyzer.py`
- 輸出：`scripts/output/processed/collaboration_scores.csv`

---

#### ☐ 步驟 10：實作技術廣度分析器 (18% 權重)
**為什麼需要**：
- 評估開發者的技術棧覆蓋範圍
- 區分前端、後端、全棧、DevOps 開發者

**分析指標**：
```python
# 檔案類型分佈統計
- 前端：.js, .ts, .jsx, .tsx, .vue, .css, .scss
- 後端：.cs, .java, .py, .go, .rb
- DevOps：Dockerfile, .yml, .sh, .tf
- 其他：.md, .sql, .json

# 評分：
- 5+ 種語言：技術廣度優秀 (10分)
- 3-5 種：全棧能力 (8分)
- 1-2 種：專精型 (6分)
```

**產出**：
- `scripts/analyzers/tech_breadth_analyzer.py`
- 輸出：`scripts/output/processed/tech_breadth_scores.csv`

---

#### ☐ 步驟 11：實作程式碼貢獻量分析器 (12% 權重)
**為什麼需要**：
- 評估開發者的活躍度與產出量
- 包含提交次數、新增/刪除行數、活躍天數

**分析指標**：
```python
# 基礎統計
- 提交次數：200+ 高活躍 (10分), 100-200 穩定 (8分), <50 參與度低 (4分)
- 程式碼行數（排除自動生成檔案）
- 活躍天數
- 涉及檔案數
```

**產出**：
- `scripts/analyzers/contribution_analyzer.py`
- 輸出：`scripts/output/processed/contribution_scores.csv`

---

#### ☐ 步驟 12：實作工作模式分析器 (10% 權重)
**為什麼需要**：
- 評估開發者的工作習慣與效率
- 包含時間分佈、提交穩定性

**分析指標**：
```python
# 時間分佈
- 工作日 vs 週末提交比例
- 工作時段 (9-18點) vs 深夜 (22-6點) 提交比例

# 評分：
- 工作日集中、工作時段 >60%：優秀
- 深夜/凌晨頻繁、週末集中爆量：時間管理問題
```

**產出**：
- `scripts/analyzers/work_pattern_analyzer.py`
- 輸出：`scripts/output/processed/work_pattern_scores.csv`

---

#### ☐ 步驟 13：實作進步趨勢分析器 (15% 權重)
**為什麼需要**：
- 評估開發者的成長曲線
- 對比早期與近期表現

**分析邏輯**：
```python
# 時間切片對比（前 6 個月 vs 最近 6 個月）
- Commit message 品質提升
- 變更規模合理化
- 技術棧擴展
- 修復率降低

# 計算進步指數
進步指數 = (近期得分 - 早期得分) / 早期得分 * 100%
```

**產出**：
- `scripts/analyzers/progress_analyzer.py`
- 輸出：`scripts/output/processed/progress_scores.csv`

---

### 🔹 階段四：報告生成模組 (預估時間：完成後標記)

#### ☐ 步驟 14：實作綜合評分計算器
**為什麼需要**：
- 整合所有分析器的結果
- 根據 `analysis-spec.md` 的權重公式計算總分

**評分公式**：
```python
總分 = (貢獻量得分 × 0.12) +
       (品質得分 × 0.23) +
       (技術廣度得分 × 0.18) +
       (協作得分 × 0.12) +
       (Code Review 得分 × 0.10) +
       (工作模式得分 × 0.10) +
       (進步趨勢得分 × 0.15)
```

**分級判定**：
- 8-10 分：🏆 高級工程師
- 5-7 分：⭐ 中級工程師
- 1-4 分：🌱 初級工程師

**產出**：
- `scripts/analyzers/score_calculator.py`
- 輸出：`scripts/output/processed/final_scores.csv`

---

#### ☐ 步驟 15：實作 Markdown 報告生成器
**為什麼需要**：
- 產生易讀的個人技術評估報告
- 包含雷達圖、趨勢圖、詳細數據表

**報告結構**：
```markdown
# 開發者技術評估報告

## 📊 綜合評分：8.3 分（高級工程師）

## 🎯 各維度得分
[雷達圖]

## 📈 進步趨勢
[時間序列圖]

## 💡 詳細分析
### 1. Commit 品質 (23% 權重)
- Message 規範率：85% ✅
- 小型變更佔比：72% ✅
- 修復率：12% ✅

### 2. Code Review 品質 (10% 權重)
...

## 🎓 改進建議
- ✅ 優勢：...
- ⚠️ 待改進：...
```

**產出**：
- `scripts/reporters/markdown_reporter.py`
- 輸出：`scripts/output/reports/{developer_name}_report.md`

---

#### ☐ 步驟 16：實作 CSV 批次匯出器
**為什麼需要**：
- 方便用 Excel 或其他工具進行二次分析
- 支援團隊對比、排名統計

**產出**：
- `scripts/reporters/csv_exporter.py`
- 輸出：`scripts/output/all-user.contribution.csv`
- 輸出：`scripts/output/all-user.commit_quality.csv`
- 輸出：`scripts/output/all-user.review_quality.csv`
- 輸出：`scripts/output/all-user.final_scores.csv`

---

#### ☐ 步驟 17：實作視覺化圖表生成器
**為什麼需要**：
- 雷達圖：展示各維度得分
- 時間序列圖：展示進步趨勢
- 長條圖：團隊對比

**使用套件**：
- matplotlib / plotly
- 輸出格式：PNG 或 HTML（可互動）

**產出**：
- `scripts/reporters/visualization.py`
- 輸出：`scripts/output/reports/{developer_name}_radar.png`
- 輸出：`scripts/output/reports/{developer_name}_trend.png`

---

### 🔹 階段五：主程式與自動化 (預估時間：完成後標記)

#### ☐ 步驟 18：實作主程式入口
**為什麼需要**：
- 整合所有模組，提供統一的命令列介面
- 支援不同的執行模式（單人分析、批次分析、增量更新）

**命令列介面**：
```bash
# 分析單一開發者
python main.py analyze --user "開發者名稱" --from "2024-01-01" --to "2024-12-31"

# 批次分析所有開發者
python main.py analyze-all --from "2024-01-01" --to "2024-12-31"

# 產生團隊匯總報告
python main.py team-report --from "2024-01-01" --to "2024-12-31"

# 增量更新數據
python main.py update --since "2024-12-01"
```

**產出**：
- `scripts/main.py`

---

#### ☐ 步驟 19：撰寫使用文檔
**為什麼需要**：
- 說明如何設定 GitLab Token
- 說明各項分析參數的調整方法
- 提供常見問題排解

**產出**：
- `README.md`
- `SETUP.md`（環境設定指南）
- `API_USAGE.md`（API 使用說明）

---

#### ☐ 步驟 20：測試與驗證
**為什麼需要**：
- 確保數據收集的完整性
- 驗證評分結果的合理性
- 測試錯誤處理與邊界情況

**測試項目**：
- GitLab API 連線測試
- 數據合併邏輯測試
- 評分計算測試
- 報告生成測試

**產出**：
- `scripts/tests/` 測試目錄
- 測試報告

---

## 🚀 執行順序建議

1. **先建立 步驟 1-3**（環境準備），確認配置正確
2. **實作 步驟 4-6**（數據收集），確保能取得完整數據
3. **實作 步驟 7-13**（分析模組），每個分析器獨立測試
4. **實作 步驟 14-17**（報告生成），驗證報告格式
5. **實作 步驟 18-20**（整合與測試）

---

## ⚠️ 風險與注意事項

### 1. GitLab API Rate Limiting
**風險**：GitLab.com 的 API 有速率限制（每分鐘 300 次請求）
**對策**：
- 實作請求節流（throttling）
- 使用 pagination 減少請求次數
- 本地快取已取得的數據

### 2. 數據量過大
**風險**：大型專案的 commit 和 MR 數量可能非常龐大
**對策**：
- 支援時間範圍篩選
- 支援專案白名單/黑名單
- 增量更新而非全量抓取

### 3. 多身份識別問題
**風險**：同一開發者可能有多個 email 或 username
**對策**：
- 建立身份映射表（手動維護）
- 使用 GitLab API 的 user ID 作為唯一識別

### 4. Code Review 深度自動判斷
**風險**：很難自動判斷 Review Comment 的品質
**對策**：
- 階段一：使用關鍵字規則匹配
- 階段二（可選）：整合 LLM API 進行語義分析

---

## 📚 依賴套件清單

```python
# requirements.txt
python-gitlab>=4.0.0       # GitLab API 客戶端
requests>=2.31.0           # HTTP 請求
pandas>=2.0.0              # 數據處理
matplotlib>=3.7.0          # 圖表繪製
plotly>=5.14.0             # 互動式圖表
python-dotenv>=1.0.0       # 環境變數管理
click>=8.1.0               # 命令列介面
tqdm>=4.65.0               # 進度條顯示
pyyaml>=6.0                # YAML 配置檔解析
```

---

## 📅 預估完成時間（僅供參考）

| 階段 | 步驟 | 預估時間 |
|------|------|----------|
| 環境準備 | 1-3 | 0.5 天 |
| 數據收集 | 4-6 | 2 天 |
| 分析模組 | 7-13 | 3 天 |
| 報告生成 | 14-17 | 1.5 天 |
| 整合測試 | 18-20 | 1 天 |
| **總計** | | **8 天** |

---

## ✅ 下一步行動

請確認此實作計畫是否符合您的需求。確認後，我將：
1. 建立 `GitLab開發者分析系統實作計畫.Progress.md` 進度追蹤檔案
2. 開始從**步驟 1**開始實作
3. 每完成一個步驟，等待您的確認後再繼續下一步

---

**建立日期**：2026-01-13
**預計完成**：2026-01-21（依實際進度調整）
