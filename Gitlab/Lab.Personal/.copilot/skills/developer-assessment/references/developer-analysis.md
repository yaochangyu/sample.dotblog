# GitLab 開發者程式碼品質與技術水平分析

## 📊 概述

本工具整合 GitLab API 的「開發者活動與貢獻資料」，透過 `gl-cli.py user-details` 命令提供完整的開發者分析報告。

## 🎯 分析目標

### 核心分析指標

#### 1️⃣ 開發者基本資訊
- 使用者個人資料（姓名、Email、組織等）
- 帳號狀態與權限
- 社交資訊（追蹤者、追蹤中）
- 最後活動時間

#### 2️⃣ Commit 品質分析
- 提交頻率（總提交數）
- 程式碼變更量（新增/刪除行數）
- 平均每次提交的變更量
- 提交時間分布

#### 3️⃣ Merge Request 品質
- MR 總數與合併率
- MR 規模（變更檔案數）
- Code Review 參與度
- 討論與評論數量

#### 4️⃣ Code Review 能力
- 提出的 review comments 數量
- 參與 review 的專案數
- 解決問題的能力

#### 5️⃣ 專案貢獻統計
- 貢獻者統計（來自 GitLab repository_contributors API）
- 總 commits 數
- 新增/刪除行數
- 跨專案貢獻度

#### 6️⃣ 使用者活動事件
- Push 事件
- MR 創建與合併事件
- Issue 相關活動
- 其他專案互動

---

## 📋 輸出檔案說明

### CSV 檔案清單

執行 `user-details` 命令後，會產生以下 CSV 檔案：

| 檔案名稱 | 說明 | 主要欄位 |
|---------|------|---------|
| `{username}-user-user_profile.csv` | 使用者基本資訊 | user_id, username, name, email, state, created_at, last_activity_on, is_admin, organization, job_title, location, bio, followers, following |
| `{username}-user-user_events.csv` | 使用者活動事件 | event_id, action_name, target_type, target_title, created_at, project_id, push_data |
| `{username}-user-contributors.csv` | 貢獻者統計（來自 GitLab API） | project_id, project_name, contributor_name, contributor_email, total_commits, total_additions, total_deletions |
| `{username}-user-commits.csv` | Commit 詳細記錄 | commit_id, author_name, author_email, committed_date, title, additions, deletions, total, project_name |
| `{username}-user-code_changes.csv` | 程式碼異動詳情 | commit_id, file_path, old_path, new_path, new_file, renamed_file, deleted_file, project_name |
| `{username}-user-merge_requests.csv` | Merge Request 記錄 | mr_iid, title, state, author, created_at, merged_at, source_branch, target_branch, upvotes, discussion_count |
| `{username}-user-code_reviews.csv` | Code Review 評論 | mr_iid, author, created_at, body, type, resolvable, resolved, project_name |
| `{username}-user-permissions.csv` | 專案授權資訊 | project_name, member_name, member_username, member_email, access_level, access_level_name, expires_at |
| `{username}-user-statistics.csv` | 統計摘要 | author_name, author_email, total_commits, total_additions, total_deletions, avg_changes_per_commit, total_merge_requests, merged_mrs, total_code_reviews, total_files_changed, projects_contributed, total_projects_with_access, contributor_total_commits, total_user_events |

---

## 🚀 使用範例

### 1. 取得特定開發者的完整資料

```bash
python gl-cli.py user-details --username alice --start-date 2024-01-01 --end-date 2024-12-31
```

**輸出檔案：**
- `alice-user-user_profile.csv` - Alice 的基本資訊
- `alice-user-user_events.csv` - Alice 的活動事件
- `alice-user-contributors.csv` - Alice 的貢獻者統計
- `alice-user-commits.csv` - Alice 的所有 commits
- `alice-user-code_changes.csv` - Alice 的程式碼異動
- `alice-user-merge_requests.csv` - Alice 的 MR 記錄
- `alice-user-code_reviews.csv` - Alice 的 code review
- `alice-user-permissions.csv` - Alice 的專案授權
- `alice-user-statistics.csv` - Alice 的統計摘要

### 2. 取得多位開發者的資料

```bash
python gl-cli.py user-details --username alice bob charlie --start-date 2024-01-01
```

### 3. 取得特定專案的開發者活動

```bash
python gl-cli.py user-details --project-name "web-api" --start-date 2024-01-01
```

**輸出檔案：**
- `web-api-users-user_profile.csv`
- `web-api-users-user_events.csv`
- `web-api-users-contributors.csv`
- `web-api-users-commits.csv`
- `web-api-users-code_changes.csv`
- `web-api-users-merge_requests.csv`
- `web-api-users-code_reviews.csv`
- `web-api-users-permissions.csv`
- `web-api-users-statistics.csv`

### 4. 組合查詢：多位使用者在多個專案

```bash
python gl-cli.py user-details --username alice bob --project-name "web-api" "mobile-app" --start-date 2024-01-01
```

### 5. 取得所有使用者的資料

```bash
python gl-cli.py user-details --start-date 2024-01-01 --end-date 2024-12-31
```

**輸出檔案：**
- `all-users-user_profile.csv`
- `all-users-user_events.csv`
- `all-users-contributors.csv`
- `all-users-commits.csv`
- `all-users-code_changes.csv`
- `all-users-merge_requests.csv`
- `all-users-code_reviews.csv`
- `all-users-permissions.csv`
- `all-users-statistics.csv`

---

## 📊 統計摘要欄位說明

`*-statistics.csv` 檔案包含以下重要指標：

### 基本資訊
- **author_name**: 開發者名稱
- **author_email**: 開發者 Email

### Commit 統計
- **total_commits**: 總提交次數
- **total_additions**: 總新增行數
- **total_deletions**: 總刪除行數
- **total_changes**: 總變更行數
- **avg_changes_per_commit**: 平均每次提交的變更行數

### Merge Request 統計
- **total_merge_requests**: 總 MR 數量
- **merged_mrs**: 已合併的 MR 數量

### Code Review 統計
- **total_code_reviews**: 參與的 code review 評論數
- **total_files_changed**: 變更的檔案總數

### 專案貢獻統計
- **projects_contributed**: 貢獻的專案數量
- **total_projects_with_access**: 有權限的專案數量
- **owner_projects**: Owner 權限的專案數
- **maintainer_projects**: Maintainer 權限的專案數
- **developer_projects**: Developer 權限的專案數
- **reporter_projects**: Reporter 權限的專案數
- **guest_projects**: Guest 權限的專案數

### 貢獻者 API 統計（來自 repository_contributors）
- **contributor_total_commits**: 貢獻者總 commits（由 GitLab API 統計）
- **contributor_total_additions**: 貢獻者總新增行數
- **contributor_total_deletions**: 貢獻者總刪除行數

### 活動事件統計
- **total_user_events**: 使用者事件總數

---

## 🎯 開發者品質評分建議

### 評分指標權重（參考）

| 指標 | 權重 | 說明 |
|-----|------|------|
| **Commit 品質** | 25% | 基於 avg_changes_per_commit（適中為佳，過大或過小都需檢視） |
| **MR 合併率** | 20% | merged_mrs / total_merge_requests |
| **Code Review 參與度** | 20% | total_code_reviews / projects_contributed |
| **專案貢獻廣度** | 15% | projects_contributed |
| **程式碼變更量** | 10% | total_additions + total_deletions |
| **活躍度** | 10% | total_user_events |

### 品質等級參考

- **🌟 優秀 (9-10分)**：全方位高品質開發者
  - MR 合併率 > 90%
  - 平均 Code Review 評論 > 5 per project
  - 跨專案貢獻 > 3 個
  
- **✅ 良好 (7-8分)**：穩定可靠的開發者
  - MR 合併率 > 75%
  - 有一定的 Code Review 參與
  - 專注於核心專案
  
- **⚠️ 需改進 (5-6分)**：技術能力待提升
  - MR 合併率 < 75%
  - Code Review 參與度低
  - 程式碼品質需加強
  
- **❌ 不足 (<5分)**：需要培訓與輔導
  - MR 合併率 < 50%
  - 缺乏 Code Review 參與
  - 活躍度低

---

## 📈 進階分析建議

### 1. 使用 Excel/Power BI 進行視覺化

將 CSV 檔案匯入 Excel 或 Power BI，建立以下圖表：

- **趨勢圖**：Commits over time (使用 commits.csv 的 committed_date)
- **圓餅圖**：專案貢獻分布 (使用 statistics.csv 的 projects_contributed)
- **長條圖**：開發者排名比較 (使用 statistics.csv)
- **熱力圖**：檔案變更頻率 (使用 code_changes.csv)

### 2. 使用 Python 進行數據分析

```python
import pandas as pd

# 讀取統計資料
stats = pd.read_csv('alice-user-statistics.csv')

# 計算品質分數
stats['quality_score'] = (
    (stats['merged_mrs'] / stats['total_merge_requests'] * 20) +  # MR合併率
    (stats['total_code_reviews'] / stats['projects_contributed'] * 20) +  # Review參與度
    (stats['projects_contributed'] * 5) +  # 專案廣度
    # ... 其他指標
)

print(stats[['author_name', 'quality_score']].sort_values('quality_score', ascending=False))
```

### 3. 團隊比較分析

```bash
# 產生多位開發者的資料
python gl-cli.py user-details --username alice bob charlie --start-date 2024-01-01

# 使用 pandas 合併分析
import pandas as pd

alice_stats = pd.read_csv('alice-user-statistics.csv')
bob_stats = pd.read_csv('bob-user-statistics.csv')
charlie_stats = pd.read_csv('charlie-user-statistics.csv')

all_stats = pd.concat([alice_stats, bob_stats, charlie_stats])
print(all_stats.describe())
```

---

## 🔗 相關章節參考

### CSV 檔案參考

- [使用者基本資訊 CSV](#) - `*-user_profile.csv`
- [使用者活動事件 CSV](#) - `*-user_events.csv`
- [貢獻者統計 CSV](#) - `*-contributors.csv`
- [Commit 記錄 CSV](#) - `*-commits.csv`
- [程式碼異動 CSV](#) - `*-code_changes.csv`
- [Merge Request CSV](#) - `*-merge_requests.csv`
- [Code Review CSV](#) - `*-code_reviews.csv`
- [專案授權 CSV](#) - `*-permissions.csv`
- [統計摘要 CSV](#) - `*-statistics.csv`

### 技術文件參考

- [GitLab API 開發者資料完整指南](./gitlab-api.md)
- [開發者活動與貢獻資料](./gitlab-api.md#開發者活動與貢獻資料)
- [使用者基本資訊](./gitlab-api.md#使用者基本資訊-user-profile)
- [實戰範例](./gitlab-api.md#實戰範例完整開發者檔案)

---

## ⚠️ 注意事項

1. **資料隱私**：部分使用者可能隱藏 Email，只能取得公開資訊
2. **API 權限**：需要至少 `read_api` 或 `read_repository` scope
3. **資料完整性**：確保 Git 設定的 email 與 GitLab 帳號 email 一致，以便準確匹配
4. **效能考量**：大型專案或長時間範圍的查詢可能需要較長時間
5. **時間範圍**：建議使用適當的時間範圍（如季度或年度），避免資料過於龐大

---

## 📚 附錄

### 常見問題 (FAQ)

**Q: 為什麼找不到某位開發者的資料？**

A: 可能原因：
- Git 設定的名稱/email 與 GitLab 帳號不符
- 使用者在指定時間範圍內沒有活動
- 使用者沒有權限存取的專案

**Q: CSV 檔案是空的怎麼辦？**

A: 檢查：
- 時間範圍是否正確
- 使用者名稱是否正確（區分大小寫）
- 專案名稱是否正確
- 是否有足夠的 API 權限

**Q: 如何提升查詢效率？**

A: 建議：
- 指定特定的專案名稱，而非查詢所有專案
- 縮小時間範圍
- 指定特定使用者，而非查詢所有使用者
- 使用專案 ID 而非專案名稱

---

**最後更新**: 2026-01-16  
**版本**: 2.0  
**作者**: GitLab CLI Team
