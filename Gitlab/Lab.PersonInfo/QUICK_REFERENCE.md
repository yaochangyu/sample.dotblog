# 📋 快速參考卡

## 🚀 一鍵執行命令

### 分析單一開發者
```bash
# 完整報告（推薦）
python3 developer_analyzer.py "開發者名稱" "2024-01-01" "2024-12-31"

# 快速統計
./analyze_developer.sh "開發者名稱" "2024-01-01" "2024-12-31"
```

### 分析整個團隊
```bash
./analyze_all_developers.sh "2024-01-01" "2024-12-31"
```

### 進步趨勢分析
```bash
python3 progress_analyzer.py "開發者" "2024-01-01" "2024-06-30" "2024-07-01" "2024-12-31"
```

---

## 📊 評估維度速查

| 維度 | 權重 | 優秀標準 |
|------|------|----------|
| 貢獻量 | 15% | 200+ 提交 |
| **品質** | **25%** | 規範率 >80%、小型變更 >60%、修復率 <15% |
| 技術廣度 | 20% | 5+ 種語言 |
| 協作 | 15% | Revert 率 <2% |
| 工作模式 | 10% | 工作時段 >60% |
| 進步趨勢 | 15% | 持續改善 |

---

## 🎯 快速改進指南

### 立即可做（1 週內）
1. ✅ 採用 Conventional Commits：`feat: `, `fix: `, `docs: `
2. ✅ Commit message 長度：50-72 字元
3. ✅ 提交前檢查：`git diff`

### 中期改進（1-3 月）
1. ✅ 拆分 commit（一個 commit 一件事）
2. ✅ 增加測試覆蓋
3. ✅ 定期重構

### 長期提升（6 月+）
1. ✅ 擴展技術棧
2. ✅ 參與架構設計
3. ✅ Code Review

---

## 🔍 常用 Git 命令

```bash
# 查看自己的統計
git log --author="$(git config user.name)" --oneline | wc -l

# 查看程式碼變更
git log --author="$(git config user.name)" --shortstat

# 檢查 commit message 規範
git log --oneline --author="$(git config user.name)" | head -20

# 查看檔案類型分佈
git log --author="$(git config user.name)" --name-only --pretty=format: | 
  grep -o '\.[^.]*$' | sort | uniq -c | sort -rn
```

---

## 📈 評分解讀

| 分數 | 等級 | 說明 |
|------|------|------|
| 8-10 | 🏆 高級 | 優秀，繼續保持 |
| 5-7  | ⭐ 中級 | 良好，有提升空間 |
| 1-4  | 🌱 初級 | 需要改進 |

---

## 📁 輸出檔案說明

| 檔案 | 說明 |
|------|------|
| `[開發者]_YYYYMMDD.md` | 個人詳細評估報告 |
| `[開發者]_progress_YYYYMMDD.md` | 進步趨勢分析 |
| `summary_YYYYMMDD.md` | 團隊匯總報告 |

---

## 💡 提醒

- ✅ 數據僅供參考，不是 KPI
- ✅ 結合 Code Review 和業務成果
- ✅ 關注進步趨勢，不只看分數
- ⚠️ 避免為指標而優化指標
