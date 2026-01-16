---
name: developer-assessment
description: GitLab 開發者評估與分析技能，使用 gl-cli.py 工具分析開發者的程式碼品質、提交歷史、專案參與度與技術水平，並產生綜合評估報告。
globs: ['**/*.py', '**/*.md', '**/scripts/**']
---

# GitLab Developer Assessment Skill

GitLab 開發者評估與分析專家，透過 `gl-cli.py` 工具深度分析開發者在 GitLab 上的活動與貢獻，提供多維度的技術能力評估。

## 核心職責

1. **互動式需求確認**
   - 詢問分析目標（特定開發者 vs 團隊整體）
   - 確認分析範圍（時間區間、專案範圍）
   - 選擇評估維度（程式碼品質、活躍度、協作能力等）

2. **資料收集與分析**
   - 使用 `gl-cli.py user-stats` 取得開發者統計資訊
   - 使用 `gl-cli.py user-projects` 取得開發者專案列表
   - 使用 `gl-cli.py project-stats` 取得專案詳細資訊
   - 使用 `gl-cli.py group-stats` 取得群組資訊（團隊分析）

3. **多維度評估**
   - **提交品質**：分析 commit 頻率、commit message 規範、程式碼變更量
   - **活躍度**：評估提交頻率、MR 參與度、issue 回應速度
   - **協作能力**：分析 code review 參與、跨專案協作、知識分享
   - **技術深度**：識別技術棧使用、程式語言熟練度、架構設計能力
   - **專案貢獻**：統計專案參與數量、核心專案貢獻度

4. **報告產生**
   - 產生結構化評估報告（markdown 格式）
   - 提供視覺化圖表建議（若需要）
   - 給予改善建議與學習方向

## 工具整合

### gl-cli.py 使用方式

```bash
# 環境設定
cd /mnt/d/lab/sample.dotblog/Gitlab/Lab.Personal/scripts

# 取得使用者統計資訊
python3 gl-cli.py user-stats \
  --username <開發者名稱> \
  --project-name <專案名稱> \
  --start-date <開始日期> \
  --end-date <結束日期> \
  --group-id <群組ID> \
  --output <輸出檔案.xlsx>

# 取得使用者專案列表
python3 gl-cli.py user-projects \
  --username <開發者名稱> \
  --group-name <群組名稱> \
  --output <輸出檔案.xlsx>

# 取得專案詳細資訊
python3 gl-cli.py project-stats \
  --project-name <專案名稱> \
  --group-id <群組ID> \
  --output <輸出檔案.xlsx>

# 取得群組資訊
python3 gl-cli.py group-stats \
  --group-name <群組名稱> \
  --output <輸出檔案.xlsx>
```

### 相依套件

執行前需確保已安裝：
- pandas
- openpyxl
- urllib3
- python-gitlab (透過 gitlab_client.py)

若缺少套件，引導使用者安裝：
```bash
cd /mnt/d/lab/sample.dotblog/Gitlab/Lab.Personal
source .venv/bin/activate  # 如果有虛擬環境
pip install pandas openpyxl urllib3 python-gitlab
```

## 互動式工作流程

### 第 1 步：需求確認

詢問使用者：

1. **分析對象**
   - 特定開發者（請提供 GitLab username）
   - 整個團隊/群組（請提供群組名稱或 ID）

2. **時間範圍**
   - 最近 1 個月
   - 最近 3 個月
   - 最近 6 個月
   - 自訂時間區間（請提供開始與結束日期）

3. **專案範圍**
   - 所有專案
   - 特定專案（請提供專案名稱列表）
   - 特定群組下的專案（請提供群組 ID 或名稱）

4. **評估維度**（多選）
   - [ ] 提交活躍度與頻率
   - [ ] 程式碼變更量分析
   - [ ] Commit message 品質
   - [ ] 專案參與廣度
   - [ ] 技術棧識別
   - [ ] 完整評估（包含所有維度）

### 第 2 步：執行資料收集

根據使用者選擇，依序執行：

1. **檢查環境**
   ```bash
   # 確認 gl-cli.py 可用
   test -f /mnt/d/lab/sample.dotblog/Gitlab/Lab.Personal/scripts/gl-cli.py
   
   # 檢查相依套件
   cd /mnt/d/lab/sample.dotblog/Gitlab/Lab.Personal/scripts
   python3 -c "import pandas, openpyxl, urllib3" 2>&1
   ```

2. **收集開發者資料**
   ```bash
   cd /mnt/d/lab/sample.dotblog/Gitlab/Lab.Personal/scripts
   python3 gl-cli.py user-stats \
     --username <username> \
     --start-date <YYYY-MM-DD> \
     --end-date <YYYY-MM-DD> \
     --output /tmp/dev_stats_<username>.xlsx
   ```

3. **收集專案參與資料**
   ```bash
   python3 gl-cli.py user-projects \
     --username <username> \
     --output /tmp/dev_projects_<username>.xlsx
   ```

4. **讀取並解析輸出檔案**
   - 使用 bash 工具讀取生成的 Excel 檔案（若需要可轉換為 CSV）
   - 或直接查看 gl-cli.py 的 console 輸出進行分析

### 第 3 步：資料分析

根據收集的資料，分析以下指標：

#### 3.1 提交活躍度
- 總 commit 數量
- 平均每週/每月 commit 數
- 提交時間分布（識別工作習慣）
- 活躍天數佔比

#### 3.2 程式碼品質指標
- 平均每次 commit 的程式碼變更量（Lines of Code）
- 新增 vs 刪除 vs 修改的比例
- 檔案類型分布（識別專長領域）
- Commit message 規範性（長度、格式、描述性）

#### 3.3 專案貢獻度
- 參與專案數量
- 每個專案的貢獻量分布
- 核心專案識別（貢獻量 > 30% 的專案）
- 跨專案協作指標

#### 3.4 技術棧分析
- 使用的程式語言分布
- 框架與工具識別（從檔案類型推斷）
- 技術廣度 vs 深度評估

### 第 4 步：產生評估報告

產生結構化的 Markdown 報告：

```markdown
# GitLab Developer Assessment Report

## 📊 基本資訊
- **開發者**: <username>
- **分析期間**: <start_date> ~ <end_date>
- **分析專案**: <project_count> 個專案
- **報告生成時間**: <timestamp>

---

## 🎯 綜合評分

| 評估維度 | 評分 | 說明 |
|---------|------|------|
| 提交活躍度 | ⭐⭐⭐⭐☆ (4/5) | 平均每週 X 次提交，表現良好 |
| 程式碼品質 | ⭐⭐⭐⭐⭐ (5/5) | Commit message 規範，變更量合理 |
| 專案貢獻度 | ⭐⭐⭐☆☆ (3/5) | 主要集中在 2 個核心專案 |
| 技術廣度 | ⭐⭐⭐⭐☆ (4/5) | 熟悉多種技術棧 |

**總體評價**: ⭐⭐⭐⭐☆ (4/5)

---

## 📈 活躍度分析

- **總 Commit 數**: X 次
- **平均每週提交**: X 次
- **活躍天數**: X 天 (佔比 X%)
- **最活躍時段**: 週一至週五 9:00-18:00

### 趨勢圖建議
\`\`\`
[建議使用 Excel 開啟輸出檔案，查看詳細統計圖表]
\`\`\`

---

## 💻 程式碼貢獻分析

- **總變更行數**: +X / -Y 行
- **平均每次 Commit**: ~Z 行
- **主要變更類型**: 
  - 新增功能: X%
  - Bug 修復: Y%
  - 重構: Z%

### Commit Message 品質
- ✅ 規範性: 良好
- ✅ 描述性: 詳細
- ⚠️ 改善建議: 建議增加 ticket ID 引用

---

## 🚀 專案參與度

| 專案名稱 | Commit 數 | 貢獻度 | 角色 |
|---------|----------|--------|------|
| Project A | X | High | Core Developer |
| Project B | Y | Medium | Contributor |
| Project C | Z | Low | Occasional |

**核心專案**: Project A (貢獻度 X%)

---

## 🔧 技術棧分析

### 程式語言分布
- Python: X%
- JavaScript: Y%
- SQL: Z%
- Others: W%

### 專長領域
- ✅ 後端開發 (Python, API)
- ✅ 資料庫設計 (SQL)
- 🔄 前端開發 (JavaScript) - 持續學習中

---

## 💡 改善建議

1. **提升專案參與廣度**
   - 建議參與更多跨團隊協作專案
   - 嘗試不同領域的技術挑戰

2. **深化技術深度**
   - 在核心專案中承擔更多架構設計工作
   - 增加 code review 參與度

3. **知識分享**
   - 建議撰寫技術文件或 README
   - 參與 issue 討論，分享經驗

---

## 📌 數據來源

本報告資料來源於 GitLab，透過 `gl-cli.py` 工具收集：
- 使用者統計: `user-stats`
- 專案列表: `user-projects`
- 時間範圍: <start_date> ~ <end_date>

---

**報告結束**
\`\`\`

### 第 5 步：後續行動建議

詢問使用者是否需要：

1. **匯出詳細資料**
   - Excel 檔案已儲存在: `/tmp/dev_stats_<username>.xlsx`
   - 是否需要轉換為 CSV 或其他格式？

2. **進階分析**
   - 與團隊平均值比較
   - 時間序列趨勢分析
   - 特定專案深度分析

3. **持續追蹤**
   - 設定定期評估機制
   - 建立績效追蹤儀表板

## 錯誤處理

### 常見問題處理

1. **缺少相依套件**
   ```bash
   # 引導安裝
   cd /mnt/d/lab/sample.dotblog/Gitlab/Lab.Personal
   source .venv/bin/activate
   pip install -r requirements.txt
   ```

2. **GitLab 連線失敗**
   - 檢查 `config.py` 設定
   - 確認 GitLab Token 有效性
   - 驗證網路連線與 SSL 憑證

3. **無資料返回**
   - 確認使用者名稱正確
   - 檢查時間範圍是否合理
   - 驗證使用者在指定專案中有活動

4. **權限不足**
   - 確認 GitLab Token 有足夠權限（read_api, read_repository）
   - 檢查使用者是否為專案成員

## 最佳實踐

1. **資料保護**
   - 輸出檔案使用臨時目錄 (`/tmp`)
   - 不在報告中包含敏感資訊（email、token）
   - 分析完成後詢問是否刪除暫存檔案

2. **效能優化**
   - 大型團隊分析時，建議分批處理
   - 使用 `--group-id` 限縮查詢範圍
   - 避免過長的時間區間（建議 ≤ 6 個月）

3. **報告品質**
   - 提供具體數據支持評分
   - 避免主觀判斷，基於客觀指標
   - 給予建設性改善建議

4. **使用者體驗**
   - 清晰的互動提示
   - 進度即時反饋
   - 結果易於理解的視覺化呈現

## 範例對話流程

```
使用者: 幫我分析開發者 john.doe 的表現

Skill: 好的！我將協助您分析 john.doe 的開發表現。請回答以下問題：

1. 分析時間範圍？
   a) 最近 1 個月
   b) 最近 3 個月
   c) 最近 6 個月
   d) 自訂（請提供起迄日期）

使用者: b

Skill: 2. 專案範圍？
   a) 所有專案
   b) 特定專案（請列出專案名稱）
   c) 特定群組（請提供群組 ID）

使用者: a

Skill: 3. 評估維度？（可多選）
   a) 提交活躍度
   b) 程式碼品質
   c) 專案參與度
   d) 技術棧分析
   e) 完整評估（全部）

使用者: e

Skill: 收到！開始執行完整評估...

[執行 gl-cli.py 命令]
[分析資料]
[產生報告]

完成！以下是 john.doe 的評估報告：

[顯示 Markdown 報告]

是否需要：
- 查看詳細 Excel 報告？
- 與團隊平均值比較？
- 匯出報告為檔案？
```

## 整合其他工具

若需要進階分析，可整合：

1. **Git 本地分析** (若有 clone repository)
   - 使用 `git log` 分析 commit 歷史
   - 使用 `git blame` 分析程式碼擁有權
   - 使用 `git diff` 分析變更品質

2. **程式碼品質工具**
   - pylint / flake8 (Python)
   - eslint (JavaScript)
   - SonarQube

3. **視覺化工具**
   - matplotlib / seaborn (Python 圖表)
   - Excel 樞紐分析表
   - Grafana (若有 metrics)

## 限制與注意事項

1. **資料準確性**
   - 依賴 GitLab API 回傳資料
   - 可能受網路延遲影響
   - 歷史資料可能不完整（取決於 GitLab 設定）

2. **評估侷限**
   - 無法評估程式碼實際品質（需 code review）
   - 無法評估軟技能（溝通、領導力）
   - 量化指標不代表全部價值

3. **隱私考量**
   - 需取得開發者同意
   - 不應作為唯一績效評估依據
   - 報告應妥善保管，避免外洩

---

**使用此 Skill 時，請確保已設定好 GitLab 環境並擁有適當權限。**
