"""
GitLab API 數據收集器

透過 GitLab API 收集專案、Merge Request、Review Comments、Commit 等數據。
"""

import os
import time
import csv
from pathlib import Path
from typing import List, Dict, Optional, Any
from datetime import datetime
from tqdm import tqdm
import pandas as pd
import gitlab
from gitlab.v4.objects import Project, MergeRequest

from config.gitlab_config import get_gitlab_config, get_gitlab_client


class GitLabAPICollector:
    """GitLab API 數據收集器"""

    def __init__(self, output_dir: Optional[str] = None):
        """
        初始化收集器

        Args:
            output_dir: 輸出目錄路徑，預設為 scripts/output/raw/
        """
        self.config = get_gitlab_config()
        self.gl = get_gitlab_client()

        # 設定輸出目錄
        if output_dir:
            self.output_dir = Path(output_dir)
        else:
            self.output_dir = Path(__file__).parent.parent / "output" / "raw"

        self.output_dir.mkdir(parents=True, exist_ok=True)

    def _rate_limit_wait(self):
        """API 請求節流"""
        self.config.rate_limit_wait()

    def _retry_on_error(self, func, *args, max_retries: int = 3, **kwargs):
        """
        錯誤重試機制

        Args:
            func: 要執行的函數
            max_retries: 最大重試次數
            *args, **kwargs: 函數參數

        Returns:
            函數執行結果
        """
        for attempt in range(max_retries):
            try:
                return func(*args, **kwargs)
            except gitlab.exceptions.GitlabError as e:
                if attempt < max_retries - 1:
                    wait_time = (attempt + 1) * 2  # 指數退避
                    print(f"⚠️ 請求失敗，{wait_time} 秒後重試... (錯誤: {e})")
                    time.sleep(wait_time)
                else:
                    print(f"❌ 達到最大重試次數，跳過此請求: {e}")
                    raise

    # ==================== 專案收集 ====================

    def collect_projects(
        self,
        visibility: Optional[str] = None,
        owned: bool = False,
        include_archived: bool = False,
    ) -> pd.DataFrame:
        """
        收集專案列表

        Args:
            visibility: 可見性過濾 ('public', 'internal', 'private')
            owned: 只取得自己擁有的專案
            include_archived: 是否包含已封存的專案

        Returns:
            專案列表 DataFrame
        """
        print("\n🔍 收集專案列表...")

        projects_data = []

        try:
            # 取得專案列表（分頁處理）
            params = {"per_page": 100}
            if visibility:
                params["visibility"] = visibility
            if owned:
                params["owned"] = True
            if not include_archived:
                params["archived"] = False

            projects = self.gl.projects.list(all=True, **params)

            print(f"找到 {len(projects)} 個專案")

            for project in tqdm(projects, desc="處理專案"):
                projects_data.append(
                    {
                        "project_id": project.id,
                        "name": project.name,
                        "path": project.path,
                        "path_with_namespace": project.path_with_namespace,
                        "description": getattr(project, "description", ""),
                        "visibility": getattr(project, "visibility", ""),
                        "created_at": getattr(project, "created_at", ""),
                        "last_activity_at": getattr(project, "last_activity_at", ""),
                        "web_url": project.web_url,
                        "default_branch": getattr(project, "default_branch", "main"),
                        "archived": getattr(project, "archived", False),
                    }
                )
                self._rate_limit_wait()

        except Exception as e:
            print(f"❌ 收集專案列表失敗: {e}")
            raise

        # 轉換為 DataFrame
        df = pd.DataFrame(projects_data)

        # 儲存到 CSV
        output_file = self.output_dir / "gitlab_projects.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 專案列表已儲存: {output_file}")

        return df

    # ==================== Merge Request 收集 ====================

    def collect_merge_requests(
        self,
        project_ids: Optional[List[int]] = None,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None,
        state: str = "all",
    ) -> pd.DataFrame:
        """
        收集 Merge Request 數據

        Args:
            project_ids: 專案 ID 列表，None 表示所有專案
            start_date: 開始日期 (ISO 8601 格式，例如 '2024-01-01')
            end_date: 結束日期 (ISO 8601 格式，例如 '2024-12-31')
            state: MR 狀態 ('all', 'opened', 'closed', 'merged')

        Returns:
            MR 列表 DataFrame
        """
        print("\n🔍 收集 Merge Request 數據...")

        # 如果沒有指定專案，取得所有專案
        if project_ids is None:
            projects_df = self.collect_projects()
            project_ids = projects_df["project_id"].tolist()

        mr_data = []

        for project_id in tqdm(project_ids, desc="處理專案 MR"):
            try:
                project = self.gl.projects.get(project_id)

                # 取得 MR 列表
                params = {"state": state, "per_page": 100}
                if start_date:
                    params["created_after"] = start_date
                if end_date:
                    params["created_before"] = end_date

                mrs = project.mergerequests.list(all=True, **params)

                for mr in mrs:
                    # 基本資訊
                    mr_info = {
                        "project_id": project_id,
                        "mr_iid": mr.iid,
                        "mr_id": mr.id,
                        "title": mr.title,
                        "description": getattr(mr, "description", ""),
                        "state": mr.state,
                        "created_at": mr.created_at,
                        "updated_at": mr.updated_at,
                        "merged_at": getattr(mr, "merged_at", None),
                        "closed_at": getattr(mr, "closed_at", None),
                        "author_username": mr.author.get("username", ""),
                        "author_name": mr.author.get("name", ""),
                        "author_id": mr.author.get("id", ""),
                        "source_branch": mr.source_branch,
                        "target_branch": mr.target_branch,
                        "web_url": mr.web_url,
                    }

                    # Assignees (可能有多個)
                    assignees = getattr(mr, "assignees", [])
                    if assignees:
                        mr_info["assignee_usernames"] = ",".join(
                            [a.get("username", "") for a in assignees]
                        )
                    else:
                        mr_info["assignee_usernames"] = ""

                    # Reviewers (可能有多個)
                    reviewers = getattr(mr, "reviewers", [])
                    if reviewers:
                        mr_info["reviewer_usernames"] = ",".join(
                            [r.get("username", "") for r in reviewers]
                        )
                    else:
                        mr_info["reviewer_usernames"] = ""

                    # 統計資訊
                    mr_info["upvotes"] = getattr(mr, "upvotes", 0)
                    mr_info["downvotes"] = getattr(mr, "downvotes", 0)
                    mr_info["user_notes_count"] = getattr(mr, "user_notes_count", 0)

                    # Diff 統計（需要額外請求）
                    try:
                        changes = mr.changes()
                        mr_info["additions"] = sum(
                            c.get("diff", "").count("\n+") for c in changes.get("changes", [])
                        )
                        mr_info["deletions"] = sum(
                            c.get("diff", "").count("\n-") for c in changes.get("changes", [])
                        )
                        mr_info["changed_files"] = len(changes.get("changes", []))
                    except:
                        mr_info["additions"] = 0
                        mr_info["deletions"] = 0
                        mr_info["changed_files"] = 0

                    # Labels
                    labels = getattr(mr, "labels", [])
                    mr_info["labels"] = ",".join(labels) if labels else ""

                    mr_data.append(mr_info)
                    self._rate_limit_wait()

            except gitlab.exceptions.GitlabGetError as e:
                print(f"⚠️ 無法存取專案 {project_id}: {e}")
                continue
            except Exception as e:
                print(f"⚠️ 處理專案 {project_id} 的 MR 時發生錯誤: {e}")
                continue

        # 轉換為 DataFrame
        df = pd.DataFrame(mr_data)

        # 儲存到 CSV
        output_file = self.output_dir / "gitlab_merge_requests.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ Merge Request 數據已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    # ==================== Review Comments 收集 ====================

    def collect_review_comments(
        self,
        project_ids: Optional[List[int]] = None,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None,
    ) -> pd.DataFrame:
        """
        收集 Review Comments 數據

        Args:
            project_ids: 專案 ID 列表，None 表示所有專案
            start_date: 開始日期
            end_date: 結束日期

        Returns:
            Review Comments DataFrame
        """
        print("\n🔍 收集 Review Comments 數據...")

        # 如果沒有指定專案，取得所有專案
        if project_ids is None:
            projects_df = self.collect_projects()
            project_ids = projects_df["project_id"].tolist()

        comments_data = []

        for project_id in tqdm(project_ids, desc="處理專案 Review Comments"):
            try:
                project = self.gl.projects.get(project_id)

                # 取得 MR 列表
                params = {"state": "all", "per_page": 100}
                if start_date:
                    params["created_after"] = start_date
                if end_date:
                    params["created_before"] = end_date

                mrs = project.mergerequests.list(all=True, **params)

                for mr in mrs:
                    try:
                        # 取得 MR 的所有 Notes (Comments)
                        notes = mr.notes.list(all=True)

                        for note in notes:
                            # 只收集非系統訊息的 notes
                            if not getattr(note, "system", False):
                                comment_info = {
                                    "project_id": project_id,
                                    "mr_iid": mr.iid,
                                    "mr_id": mr.id,
                                    "note_id": note.id,
                                    "author_username": note.author.get("username", ""),
                                    "author_name": note.author.get("name", ""),
                                    "author_id": note.author.get("id", ""),
                                    "body": note.body,
                                    "created_at": note.created_at,
                                    "updated_at": note.updated_at,
                                    "noteable_type": getattr(note, "noteable_type", ""),
                                    "resolvable": getattr(note, "resolvable", False),
                                    "resolved": getattr(note, "resolved", False),
                                }

                                # Diff Note 相關資訊
                                if hasattr(note, "position"):
                                    position = note.position
                                    if position:
                                        comment_info["diff_file_path"] = position.get(
                                            "new_path", ""
                                        )
                                        comment_info["diff_line"] = position.get("new_line", "")
                                    else:
                                        comment_info["diff_file_path"] = ""
                                        comment_info["diff_line"] = ""
                                else:
                                    comment_info["diff_file_path"] = ""
                                    comment_info["diff_line"] = ""

                                comments_data.append(comment_info)

                        self._rate_limit_wait()

                    except Exception as e:
                        print(f"⚠️ 處理 MR {mr.iid} 的 Comments 時發生錯誤: {e}")
                        continue

            except gitlab.exceptions.GitlabGetError as e:
                print(f"⚠️ 無法存取專案 {project_id}: {e}")
                continue
            except Exception as e:
                print(f"⚠️ 處理專案 {project_id} 時發生錯誤: {e}")
                continue

        # 轉換為 DataFrame
        df = pd.DataFrame(comments_data)

        # 儲存到 CSV
        output_file = self.output_dir / "gitlab_review_comments.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ Review Comments 數據已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    # ==================== Commit 收集 (API 版本) ====================

    def collect_commits(
        self,
        project_ids: Optional[List[int]] = None,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None,
        ref_name: Optional[str] = None,
    ) -> pd.DataFrame:
        """
        收集 Commit 數據（透過 API）

        Args:
            project_ids: 專案 ID 列表，None 表示所有專案
            start_date: 開始日期
            end_date: 結束日期
            ref_name: 分支名稱（預設為專案的 default branch）

        Returns:
            Commit 列表 DataFrame
        """
        print("\n🔍 收集 Commit 數據（API）...")

        # 如果沒有指定專案，取得所有專案
        if project_ids is None:
            projects_df = self.collect_projects()
            project_ids = projects_df["project_id"].tolist()

        commit_data = []

        for project_id in tqdm(project_ids, desc="處理專案 Commits"):
            try:
                project = self.gl.projects.get(project_id)

                # 取得 Commit 列表
                params = {"per_page": 100}
                if start_date:
                    params["since"] = start_date
                if end_date:
                    params["until"] = end_date
                if ref_name:
                    params["ref_name"] = ref_name

                commits = project.commits.list(all=True, **params)

                for commit in commits:
                    commit_info = {
                        "project_id": project_id,
                        "commit_sha": commit.id,
                        "short_id": commit.short_id,
                        "title": commit.title,
                        "message": commit.message,
                        "author_name": commit.author_name,
                        "author_email": commit.author_email,
                        "authored_date": commit.authored_date,
                        "committer_name": commit.committer_name,
                        "committer_email": commit.committer_email,
                        "committed_date": commit.committed_date,
                        "created_at": commit.created_at,
                        "parent_ids": ",".join(commit.parent_ids) if commit.parent_ids else "",
                        "web_url": commit.web_url,
                    }

                    # 統計資訊
                    stats = getattr(commit, "stats", {})
                    commit_info["additions"] = stats.get("additions", 0)
                    commit_info["deletions"] = stats.get("deletions", 0)
                    commit_info["total"] = stats.get("total", 0)

                    commit_data.append(commit_info)

                self._rate_limit_wait()

            except gitlab.exceptions.GitlabGetError as e:
                print(f"⚠️ 無法存取專案 {project_id}: {e}")
                continue
            except Exception as e:
                print(f"⚠️ 處理專案 {project_id} 的 Commits 時發生錯誤: {e}")
                continue

        # 轉換為 DataFrame
        df = pd.DataFrame(commit_data)

        # 儲存到 CSV
        output_file = self.output_dir / "gitlab_commits.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ Commit 數據已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    # ==================== 完整數據收集 ====================

    def collect_all(
        self,
        project_ids: Optional[List[int]] = None,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None,
    ) -> Dict[str, pd.DataFrame]:
        """
        收集所有數據（專案、MR、Comments、Commits）

        Args:
            project_ids: 專案 ID 列表，None 表示所有專案
            start_date: 開始日期
            end_date: 結束日期

        Returns:
            包含所有數據的字典
        """
        print("=" * 60)
        print("開始收集 GitLab 數據")
        print("=" * 60)

        results = {}

        # 1. 收集專案列表
        if project_ids is None:
            projects_df = self.collect_projects()
            project_ids = projects_df["project_id"].tolist()
            results["projects"] = projects_df

        # 2. 收集 Merge Requests
        results["merge_requests"] = self.collect_merge_requests(
            project_ids=project_ids, start_date=start_date, end_date=end_date
        )

        # 3. 收集 Review Comments
        results["review_comments"] = self.collect_review_comments(
            project_ids=project_ids, start_date=start_date, end_date=end_date
        )

        # 4. 收集 Commits
        results["commits"] = self.collect_commits(
            project_ids=project_ids, start_date=start_date, end_date=end_date
        )

        print("\n" + "=" * 60)
        print("✅ 所有數據收集完成！")
        print("=" * 60)
        print(f"專案數量: {len(results.get('projects', []))}")
        print(f"MR 數量: {len(results['merge_requests'])}")
        print(f"Review Comments 數量: {len(results['review_comments'])}")
        print(f"Commits 數量: {len(results['commits'])}")
        print("=" * 60)

        return results


# 測試功能（可獨立執行此模組測試）
if __name__ == "__main__":
    import sys

    print("=" * 60)
    print("GitLab API 數據收集器測試")
    print("=" * 60)

    collector = GitLabAPICollector()

    # 測試：收集專案列表
    print("\n測試 1：收集專案列表（前 5 個）")
    try:
        projects_df = collector.collect_projects()
        print(f"\n收集到 {len(projects_df)} 個專案")
        if len(projects_df) > 0:
            print("\n前 5 個專案：")
            print(projects_df.head()[["project_id", "name", "path_with_namespace"]])
    except Exception as e:
        print(f"❌ 測試失敗: {e}")
        sys.exit(1)

    print("\n" + "=" * 60)
    print("✅ 測試完成")
    print("=" * 60)
