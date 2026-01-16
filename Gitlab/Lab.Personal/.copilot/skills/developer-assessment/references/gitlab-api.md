# GitLab API 開發者資料完整指南

## 📋 目錄
- [使用者基本資訊](#使用者基本資訊-user-profile)
- [開發者活動與貢獻資料](#開發者活動與貢獻資料)
- [其他可取得的開發者資料](#其他可取得的開發者資料)
- [實戰範例](#實戰範例完整開發者檔案)
- [注意事項](#注意事項)

---

## 👤 使用者基本資訊 (User Profile)

### 可取得的屬性：

```python
user = gl.users.get(user_id)

# ========== 基本資訊 ==========
user.id                    # 使用者 ID
user.username              # 使用者名稱
user.name                  # 真實姓名
user.email                 # Email
user.public_email          # 公開 Email
user.avatar_url            # 頭像網址
user.web_url               # 個人頁面網址

# ========== 個人資料 ==========
user.bio                   # 個人簡介
user.location              # 所在地
user.organization          # 組織
user.job_title             # 職稱
user.pronouns              # 代名詞
user.website_url           # 個人網站
user.skype                 # Skype
user.linkedin              # LinkedIn
user.twitter               # Twitter

# ========== 帳號狀態 ==========
user.state                 # 狀態 (active/blocked/banned)
user.created_at            # 帳號建立時間
user.confirmed_at          # Email 確認時間
user.last_sign_in_at       # 最後登入時間
user.current_sign_in_at    # 目前登入時間
user.last_activity_on      # 最後活動日期

# ========== 權限設定 ==========
user.is_admin              # 是否為管理員
user.can_create_group      # 是否可建立群組
user.can_create_project    # 是否可建立專案
user.projects_limit        # 專案數量限制
user.external              # 是否為外部使用者
user.private_profile       # 是否為私密檔案
user.two_factor_enabled    # 是否啟用雙因素驗證

# ========== 社交資訊 ==========
user.followers             # 追蹤者數量
user.following             # 追蹤中數量

# ========== 其他 ==========
user.bot                   # 是否為機器人帳號
user.note                  # 備註 (管理員可見)
user.namespace_id          # Namespace ID
```

---

## 📊 開發者活動與貢獻資料

### 1️⃣ Commits 相關資料

```python
# 方法 1: 透過專案取得使用者的 commits (無法直接過濾 author)
commits = project.commits.list(
    ref_name='main',
    since='2024-01-01',
    until='2024-12-31',
    with_stats=True,  # ✅ 包含統計資料
    all=True
)

# 需手動過濾特定使用者
user_commits = [c for c in commits if c.author_email == 'user@email.com']

# 每個 commit 可取得：
for commit in user_commits:
    commit.id                    # Commit SHA
    commit.short_id              # 短 SHA
    commit.title                 # Commit 標題
    commit.message               # 完整訊息
    commit.author_name           # 作者名稱
    commit.author_email          # 作者 Email
    commit.authored_date         # 提交日期
    commit.committer_name        # Committer 名稱
    commit.committer_email       # Committer Email
    commit.committed_date        # Commit 日期
    commit.created_at            # 建立時間
    commit.parent_ids            # 父 commit IDs
    commit.web_url               # 網頁連結
    
    # 📈 統計資料 (需 with_stats=True)
    commit.stats.additions       # 新增行數
    commit.stats.deletions       # 刪除行數
    commit.stats.total           # 總變更行數
```

### 2️⃣ 貢獻者統計（最佳方式）

```python
# ✅ 直接取得專案所有貢獻者的統計
contributors = project.repository_contributors()

for contributor in contributors:
    contributor.name             # 貢獻者名稱
    contributor.email            # Email
    contributor.commits          # Commit 總數
    contributor.additions        # 新增行數總計
    contributor.deletions        # 刪除行數總計
```

### 3️⃣ 使用者事件 (User Events)

```python
user = gl.users.get(user_id)
events = user.events.list(
    action='pushed',       # 可選：pushed, created, merged, commented, joined
    target_type='Issue',   # 可選：Issue, MergeRequest, Project
    after='2024-01-01',
    before='2024-12-31',
    all=True
)

# 每個事件可取得：
for event in events:
    event.id                     # 事件 ID
    event.action_name            # 動作名稱 (pushed, opened, merged...)
    event.target_type            # 目標類型
    event.target_title           # 目標標題
    event.created_at             # 發生時間
    event.author                 # 作者資訊
    event.author_username        # 作者使用者名稱
    event.project_id             # 專案 ID
    event.push_data              # Push 事件的詳細資料
```

### 4️⃣ Merge Requests 資料

```python
# 列出使用者作為作者的 MR
mrs = project.mergerequests.list(
    author_username='developer_name',
    state='all',  # all, opened, closed, merged
    updated_after='2024-01-01',
    all=True
)

for mr in mrs:
    mr.id                        # MR ID
    mr.iid                       # 專案內部 ID
    mr.title                     # 標題
    mr.description               # 描述
    mr.state                     # 狀態
    mr.merged_at                 # 合併時間
    mr.closed_at                 # 關閉時間
    mr.created_at                # 建立時間
    mr.updated_at                # 更新時間
    mr.author                    # 作者資訊
    mr.assignee                  # 指派者
    mr.reviewers                 # 審查者列表
    mr.source_branch             # 來源分支
    mr.target_branch             # 目標分支
    mr.work_in_progress          # 是否為 WIP
    mr.merge_status              # 合併狀態
    mr.user_notes_count          # 評論數量
    mr.upvotes                   # 讚數
    mr.downvotes                 # 踩數
    mr.web_url                   # 網頁連結
    
    # 📊 取得 MR 的變更統計
    changes = mr.changes()
    changes['changes']           # 變更檔案列表
    for file in changes['changes']:
        file['old_path']         # 舊路徑
        file['new_path']         # 新路徑
        file['diff']             # Diff 內容
```

### 5️⃣ Code Review 參與度

```python
# 取得 MR 的討論 (comments)
mr = project.mergerequests.get(mr_iid)
discussions = mr.discussions.list(all=True)

for discussion in discussions:
    for note in discussion.attributes['notes']:
        note['author']['username']   # 評論者
        note['body']                 # 評論內容
        note['created_at']           # 評論時間
        note['resolved']             # 是否已解決
```

---

## 🔍 其他可取得的開發者資料

### 1️⃣ 使用者參與的專案

```python
# 貢獻過的專案
contributed = user.contributed_projects.list(all=True)

# 加星的專案
starred = user.starred_projects.list(all=True)

# 所屬的專案
projects = user.projects.list(all=True)
```

### 2️⃣ 群組成員資格

```python
# 使用者加入的群組
memberships = user.memberships.list(all=True)

for membership in memberships:
    membership.source_id         # 群組/專案 ID
    membership.source_type       # 類型 (Namespace/Project)
    membership.access_level      # 權限等級 (10/20/30/40/50)
```

### 3️⃣ 追蹤關係

```python
# 追蹤者
followers = user.followers_users.list(all=True)

# 追蹤中
following = user.following_users.list(all=True)
```

### 4️⃣ SSH Keys 和 GPG Keys

```python
# SSH Keys
keys = user.keys.list(all=True)

# GPG Keys
gpgkeys = user.gpgkeys.list(all=True)
```

---

## 📝 實戰範例：完整開發者檔案

```python
def get_developer_profile(gl, user_id, project_id):
    """完整取得開發者檔案"""
    
    user = gl.users.get(user_id)
    project = gl.projects.get(project_id)
    
    # 1. 基本資訊
    profile = {
        'id': user.id,
        'username': user.username,
        'name': user.name,
        'email': user.email,
        'created_at': user.created_at,
        'last_activity_on': user.last_activity_on,
    }
    
    # 2. 貢獻統計
    contributors = project.repository_contributors()
    user_contrib = next((c for c in contributors 
                        if c['email'] == user.email), None)
    
    if user_contrib:
        profile['contributions'] = {
            'commits': user_contrib['commits'],
            'additions': user_contrib['additions'],
            'deletions': user_contrib['deletions']
        }
    
    # 3. Merge Requests
    mrs = project.mergerequests.list(
        author_username=user.username,
        all=True
    )
    profile['merge_requests'] = {
        'total': len(mrs),
        'merged': len([mr for mr in mrs if mr.state == 'merged'])
    }
    
    # 4. 活動事件
    events = user.events.list(all=True)
    profile['recent_events'] = len(events)
    
    return profile
```

---

## ⚠️ 注意事項

1. **無法直接按作者過濾 commits** - 需先取得所有 commits 再手動過濾
2. **貢獻者統計最高效** - `repository_contributors()` 直接提供統計摘要
3. **需適當權限** - 至少需要 `read_api` 或 `read_repository` scope
4. **Email 可能隱藏** - 部分使用者會隱藏 Email，需用 `commit_email` 或 `public_email`

---

## 🔑 Access Token 權限總覽

| **操作目標** | **所需 Scope** | **最低 Access Level** |
|------------|--------------|---------------------|
| 讀取 commits | `read_api` 或 `read_repository` | **Reporter (20)** |
| 讀取 MR 和 diff | `read_api` | **Reporter (20)** |
| 讀取 discussions | `read_api` | **Reporter (20)** |
| 新增專案成員 | `api` | **Maintainer (40)** |
| 管理 project tokens | `api` | **Maintainer (40)** |
| 讀取使用者事件 | `read_api` | **Reporter (20)** |
| 讀取專案統計 | `read_api` | **Reporter (20)** |

---

## 📊 Access Token Scopes 說明

常見的 scopes 包括：
- **`api`** - 完整 API 存取權限（讀寫）
- **`read_api`** - 唯讀 API 存取
- **`read_repository`** - 讀取 Repository 內容
- **`write_repository`** - 寫入 Repository（推送程式碼）
- **`read_user`** - 讀取使用者資訊
- **`sudo`** - 以其他使用者身份執行操作（需 Admin 權限）

---

## 👥 GitLab Access Level 定義

GitLab 定義五種存取層級：
- **10 - GUEST** (訪客)：只能查看
- **20 - REPORTER** (報告者)：可建立 Issue
- **30 - DEVELOPER** (開發者)：可推送程式碼、合併分支
- **40 - MAINTAINER** (維護者)：可管理專案設定
- **50 - OWNER** (擁有者)：完整控制權

### 加入開發者到專案範例

```python
import gitlab

gl = gitlab.Gitlab('https://gitlab.example.com', private_token='YOUR_TOKEN')
project = gl.projects.get(123)

# 加入開發者到專案
member = project.members.create({
    'user_id': user_id,
    'access_level': gitlab.const.DEVELOPER_ACCESS  # 30
})

# 更新成員權限
member.access_level = gitlab.const.MAINTAINER_ACCESS  # 40
member.save()

# 移除成員
member.delete()
```

---

## 📈 開發者程式碼品質分析指標

### 核心分析指標

#### 1️⃣ Commit 品質分析
- 提交頻率（每週/每月 commits 數）
- Commit message 品質（是否遵循規範）
- 新增/刪除行數比例（with_stats=True）
- 提交時間分布（工作時間 vs 非工作時間）

#### 2️⃣ Merge Request 品質
- MR 規模（變更檔案數、行數）
- Code Review 參與度（comments 數量）
- MR 週期（創建到合併時間）
- Approval 狀態和速度

#### 3️⃣ Code Review 能力
- 提出的 review comments 數量與品質
- 解決 comments 的速度
- 參與 review 的專案數

#### 4️⃣ 貢獻者統計
- 總 commits 數
- 新增行數
- 刪除行數
- 活躍時間軸

---

## 🎯 建議的 gitlab_client.py 擴充方法

```python
def get_user_events(
    self, 
    user_id: int, 
    action: Optional[str] = None,
    after: Optional[str] = None
) -> List[Any]:
    """
    取得使用者活動事件
    
    Args:
        user_id: 使用者 ID
        action: 動作類型 (pushed, created, merged 等)
        after: 起始日期 (ISO 格式)
    
    Returns:
        事件物件列表
    """
    user = self.gl.users.get(user_id)
    params = {'all': True}
    if action:
        params['action'] = action
    if after:
        params['after'] = after
    
    return user.events.list(**params)

def get_repository_contributors(self, project_id: int) -> List[Dict]:
    """
    取得專案貢獻者統計
    
    Returns:
        貢獻者統計列表（含 commits, additions, deletions）
    """
    project = self.gl.projects.get(project_id)
    return project.repository_contributors()

def get_user_commits_in_project(
    self,
    project_id: int,
    user_email: str,
    since: Optional[str] = None,
    until: Optional[str] = None
) -> List[Any]:
    """
    取得特定使用者在專案中的 commits
    
    Args:
        project_id: 專案 ID
        user_email: 使用者 Email
        since: 起始日期
        until: 結束日期
    
    Returns:
        commit 物件列表
    """
    commits = self.get_project_commits(project_id, since, until)
    return [c for c in commits if c.author_email == user_email]
```

---

## 📊 分析報表範例結構

```python
developer_report = {
    "user_id": 123,
    "username": "developer_name",
    "period": "2024-01-01 to 2024-12-31",
    "commits": {
        "total": 156,
        "additions": 12450,
        "deletions": 3890,
        "avg_per_week": 3.2
    },
    "merge_requests": {
        "total": 42,
        "merged": 38,
        "avg_review_time_hours": 18.5
    },
    "code_review": {
        "comments_made": 87,
        "mrs_reviewed": 25
    },
    "quality_score": 8.5  # 綜合評分 (0-10)
}
```

---

## 📚 相關文件

- [python-gitlab 官方文件](https://python-gitlab.readthedocs.io/)
- [GitLab API 文件](https://docs.gitlab.com/ee/api/)
- [GitLab Permissions 說明](https://docs.gitlab.com/ee/user/permissions.html)

---

## 📊 實際應用：開發者品質分析

本 API 文件的「開發者活動與貢獻資料」章節已整合至 `gl-cli.py` 工具的 `user-details` 命令。

### 產生的 CSV 檔案

執行 `user-details` 命令後，會自動產生以下 CSV 檔案供分析使用：

| CSV 檔案 | 對應 API 章節 | 說明 |
|---------|--------------|------|
| `*-user_profile.csv` | [使用者基本資訊](#使用者基本資訊-user-profile) | 包含使用者的個人資料、帳號狀態、權限設定等完整資訊 |
| `*-user_events.csv` | [使用者事件](#3️⃣-使用者事件-user-events) | 包含 push、create、merge、comment 等所有活動事件 |
| `*-contributors.csv` | [貢獻者統計](#2️⃣-貢獻者統計最佳方式) | 來自 `repository_contributors()` API 的統計資料 |
| `*-commits.csv` | [Commits 相關資料](#1️⃣-commits-相關資料) | 包含 commit SHA、作者、日期、統計數據 |
| `*-code_changes.csv` | [Commits 相關資料](#1️⃣-commits-相關資料) | 詳細的檔案變更記錄（new/modified/deleted） |
| `*-merge_requests.csv` | [Merge Requests 資料](#4️⃣-merge-requests-資料) | MR 的標題、狀態、作者、合併時間等資訊 |
| `*-code_reviews.csv` | [Code Review 參與度](#5️⃣-code-review-參與度) | MR 討論、評論、解決狀態 |
| `*-permissions.csv` | [群組成員資格](#2️⃣-群組成員資格) | 專案授權資訊、access level |
| `*-statistics.csv` | [開發者程式碼品質分析指標](#開發者程式碼品質分析指標) | 整合所有指標的統計摘要 |

### 快速開始

```bash
# 取得特定開發者的完整分析資料
python gl-cli.py user-details --username alice --start-date 2024-01-01 --end-date 2024-12-31

# 取得特定專案的所有開發者資料
python gl-cli.py user-details --project-name "web-api" --start-date 2024-01-01

# 取得所有使用者的資料
python gl-cli.py user-details --start-date 2024-01-01
```

詳細使用說明請參考：[開發者程式碼品質與技術水平分析文件](./developer-analysis.md)

---