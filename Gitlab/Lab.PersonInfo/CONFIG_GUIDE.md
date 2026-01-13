# 🎛️ 分析參數配置指南

本文件說明如何調整 `scripts/config/analysis_config.py` 中的評估參數。

---

## 📊 評估維度權重

在 `AnalysisWeights` 類別中定義各維度的權重（總和必須為 1.0）：

```python
@dataclass
class AnalysisWeights:
    contribution: float = 0.12     # 程式碼貢獻量 (12%)
    commit_quality: float = 0.23   # Commit 品質 (23%) ⭐ 最高
    tech_breadth: float = 0.18     # 技術廣度 (18%)
    collaboration: float = 0.12    # 協作能力 (12%)
    code_review: float = 0.10      # Code Review 品質 (10%)
    work_pattern: float = 0.10     # 工作模式 (10%)
    progress: float = 0.15         # 進步趨勢 (15%)
```

### 調整建議

**如果您的團隊注重程式碼品質**：
```python
commit_quality: float = 0.30  # 提高到 30%
code_review: float = 0.15     # 提高到 15%
contribution: float = 0.08    # 降低到 8%
```

**如果您的團隊注重快速交付**：
```python
contribution: float = 0.20    # 提高到 20%
work_pattern: float = 0.15    # 提高到 15%
commit_quality: float = 0.18  # 降低到 18%
```

---

## 📝 Commit 品質評分標準

### 1. Message 規範性

```python
# Conventional Commits 符合率評分標準
MESSAGE_QUALITY_THRESHOLDS = {
    "excellent": 0.80,  # >80% 符合規範：優秀 (9-10分)
    "good": 0.40,       # 40-80%：中等 (5-8分)
    # <40%：需改進 (1-4分)
}
```

**調整建議**：
- 如果團隊剛開始使用 Conventional Commits，可降低閾值：
  ```python
  "excellent": 0.60  # 降低到 60%
  "good": 0.30       # 降低到 30%
  ```

### 2. 變更粒度

```python
CHANGE_SIZE_SMALL = 100   # ≤100 行：小型變更
CHANGE_SIZE_MEDIUM = 500  # 100-500 行：中型變更
# >500 行：大型變更
```

**調整建議**：
- 前端專案（變更通常較小）：
  ```python
  CHANGE_SIZE_SMALL = 50
  CHANGE_SIZE_MEDIUM = 200
  ```
- 後端專案（變更可能較大）：
  ```python
  CHANGE_SIZE_SMALL = 150
  CHANGE_SIZE_MEDIUM = 800
  ```

### 3. 修復率

```python
FIX_RATE_THRESHOLDS = {
    "excellent": 0.15,  # <15%：優秀
    "good": 0.30,       # 15-30%：正常
    # >30%：需改進
}
```

---

## 🔍 Code Review 品質評分標準

### 1. Review 參與度

```python
PARTICIPATION_THRESHOLDS = {
    "excellent": 1.5,  # >團隊平均 1.5 倍：優秀
    "good": 1.0,       # =團隊平均：良好
    "fair": 0.5,       # 50-100% 團隊平均：中等
}
```

### 2. Review 深度

```python
# 有建議的 Review 比例評分標準
DEPTH_THRESHOLDS = {
    "excellent": 0.80,  # >80% 有具體建議：優秀
    "good": 0.50,       # 50-80%：中等
}
```

### 3. Review 時效性

```python
TIMELINESS_THRESHOLDS = {
    "excellent": 4,   # <4 小時：優秀
    "good": 24,       # 4-24 小時：良好
    "fair": 72,       # 24-72 小時：普通
}
```

**調整建議**（根據團隊時區分佈）：
- 全球分散團隊：
  ```python
  "excellent": 12   # 放寬到 12 小時
  "good": 48        # 放寬到 48 小時
  ```

---

## 📈 程式碼貢獻量評分標準

```python
COMMIT_COUNT_THRESHOLDS = {
    "high": 200,     # 200+ 次：高活躍度
    "stable": 100,   # 100-200 次：穩定貢獻
    "medium": 50,    # 50-100 次：中等參與
}
```

**調整建議**（根據分析時間範圍）：
- 分析 3 個月（而非 1 年）：
  ```python
  "high": 50
  "stable": 25
  "medium": 12
  ```

---

## 🌐 技術廣度評分標準

```python
TECH_STACK_THRESHOLDS = {
    "excellent": 5,   # 5+ 種：技術廣度優秀
    "fullstack": 3,   # 3-5 種：全棧能力
}
```

### 自訂檔案類型分類

```python
FILE_TYPE_CATEGORIES = {
    "frontend": [".js", ".ts", ".jsx", ".tsx", ".vue"],
    "backend": [".cs", ".java", ".py", ".go"],
    "database": [".sql", ".prisma", ".graphql"],
    "devops": [".yml", ".yaml", ".sh", ".tf"],
}
```

**範例：新增 Rust 後端支援**：
```python
"backend": [".cs", ".java", ".py", ".go", ".rs"],  # 加入 .rs
```

---

## 🚫 排除規則

### 1. 排除 Bot 賬號

```python
EXCLUDED_BOTS = [
    "renovate",
    "dependabot",
    "gitlab-bot",
    "github-bot",
]
```

**新增自訂 Bot**：
```python
EXCLUDED_BOTS = [
    # ... 原有的
    "your-custom-bot",
    "ci-automation",
]
```

### 2. 排除檔案模式

```python
EXCLUDED_FILE_PATTERNS = [
    "package-lock.json",
    "yarn.lock",
    "dist/*",
    "build/*",
    "node_modules/*",
]
```

**範例：排除測試資料檔**：
```python
EXCLUDED_FILE_PATTERNS = [
    # ... 原有的
    "test/fixtures/*",
    "*.test.json",
]
```

---

## 🎓 分級標準

```python
GRADE_THRESHOLDS = [
    (8.0, "🏆 高級工程師", "senior"),
    (5.0, "⭐ 中級工程師", "mid"),
    (0.0, "🌱 初級工程師", "junior"),
]
```

**調整建議**（根據團隊標準）：
- 提高門檻（更嚴格）：
  ```python
  (8.5, "🏆 高級工程師", "senior"),
  (6.5, "⭐ 中級工程師", "mid"),
  ```
- 降低門檻（更寬鬆）：
  ```python
  (7.0, "🏆 高級工程師", "senior"),
  (4.0, "⭐ 中級工程師", "mid"),
  ```

---

## ⏰ 工作模式評分標準

```python
# 工作時段定義（小時）
WORK_HOURS_START = 9
WORK_HOURS_END = 18

# 深夜時段定義（小時）
LATE_NIGHT_START = 22
LATE_NIGHT_END = 6
```

**調整建議**（根據團隊作息）：
- 彈性工時團隊：
  ```python
  WORK_HOURS_START = 10
  WORK_HOURS_END = 19
  ```
- 夜班團隊：
  ```python
  WORK_HOURS_START = 14
  WORK_HOURS_END = 23
  ```

---

## ✅ 驗證配置

修改配置後，執行以下命令驗證：

```bash
uv run python scripts/config/analysis_config.py
```

**預期輸出**：

```
============================================================
分析參數配置測試
============================================================
🔍 驗證分析參數配置...
✅ 權重配置正確（總和 = 1.0）
✅ 變更粒度閾值設定合理
✅ 工作時段設定合理
✅ 所有配置驗證通過！
```

---

## 📊 查看當前配置

```bash
uv run python -c "
from scripts.config.analysis_config import export_config_summary
import json
print(json.dumps(export_config_summary(), indent=2, ensure_ascii=False))
"
```

---

## ⚠️ 注意事項

1. **修改權重後，務必確認總和為 1.0**
   ```python
   # 使用內建驗證方法
   if not WEIGHTS.validate():
       print("❌ 權重總和不等於 1.0")
   ```

2. **閾值設定應符合實際情況**
   - 建議先使用預設值分析一次
   - 根據團隊實際數據調整閾值

3. **排除規則要謹慎設定**
   - 過度排除可能遺漏真實貢獻
   - 定期檢視排除的檔案/賬號是否合理

---

## 💡 最佳實踐

1. **先使用預設配置分析**
   - 取得團隊基準數據
   - 了解團隊的實際分佈

2. **根據團隊文化調整**
   - 敏捷團隊：提高 `contribution` 權重
   - 注重品質團隊：提高 `commit_quality` 和 `code_review` 權重

3. **定期檢視配置**
   - 每季度檢視一次
   - 根據團隊成長調整標準

4. **記錄配置變更**
   - 建議使用版控追蹤配置變更
   - 記錄變更原因

---

**文件版本**：v1.0
**最後更新**：2026-01-13
