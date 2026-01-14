"""
GitLab 資料收集器 - 收集所有開發者的程式碼品質資料
"""

import pandas as pd
from typing import List, Any, Optional
import os
from tqdm import tqdm
from base_gitlab_collector import BaseGitLabCollector

class GitLabCollector(BaseGitLabCollector):
    def __init__(
        self, 
        start_date: Optional[str] = None, 
        end_date: Optional[str] = None,
        project_ids: Optional[List[int]] = None,
        group_id: Optional[int] = None
    ):
        """
        初始化 GitLab 連線
        
        Args:
            start_date: 起始日期 (格式: YYYY-MM-DD)，預設使用 config.START_DATE
            end_date: 結束日期 (格式: YYYY-MM-DD)，預設使用 config.END_DATE
            project_ids: 指定專案 ID 列表，預設使用 config.TARGET_PROJECT_IDS
            group_id: 指定群組 ID，預設使用 config.TARGET_GROUP_ID
        """
        super().__init__(start_date, end_date, project_ids, group_id)
    
    def get_commits_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集所有開發者的 commit 資料"""
        print("\n正在收集 Commit 資料...")
        commits_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                commits = self.client.get_project_commits(
                    project.id,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    commit_detail = self.client.get_commit_detail(project.id, commit.id)
                    commits_data.append({
                        'project_id': project.id,
                        'project_name': project.name,
                        'commit_id': commit.id,
                        'commit_short_id': commit.short_id,
                        'author_name': commit.author_name,
                        'author_email': commit.author_email,
                        'committed_date': commit.committed_date,
                        'title': commit.title,
                        'message': commit.message,
                        'additions': commit_detail.stats.get('additions', 0),
                        'deletions': commit_detail.stats.get('deletions', 0),
                        'total_changes': commit_detail.stats.get('total', 0),
                        'parent_ids': ','.join(commit.parent_ids) if commit.parent_ids else '',
                        'web_url': commit.web_url
                    })
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        df = pd.DataFrame(commits_data)
        output_file = self.save_dataframe(df, "all-user.commits.csv")
        print(f"✓ Commit 資料已儲存至: {output_file}")
        return df
    
    def get_code_changes_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集程式碼異動詳細資料"""
        print("\n正在收集程式碼異動資料...")
        changes_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                commits = self.client.get_project_commits(
                    project.id,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    try:
                        diffs = self.client.get_commit_diff(project.id, commit.id)
                        
                        for diff in diffs:
                            changes_data.append({
                                'project_id': project.id,
                                'project_name': project.name,
                                'commit_id': commit.id,
                                'author_name': commit.author_name,
                                'author_email': commit.author_email,
                                'committed_date': commit.committed_date,
                                'file_path': diff.get('new_path', diff.get('old_path', '')),
                                'old_path': diff.get('old_path', ''),
                                'new_path': diff.get('new_path', ''),
                                'new_file': diff.get('new_file', False),
                                'renamed_file': diff.get('renamed_file', False),
                                'deleted_file': diff.get('deleted_file', False),
                                'diff_content': diff.get('diff', '')[:1000]  # 限制長度
                            })
                    except Exception as e:
                        continue
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        df = pd.DataFrame(changes_data)
        output_file = self.save_dataframe(df, "all-user.code-changes.csv")
        print(f"✓ 程式碼異動資料已儲存至: {output_file}")
        return df
    
    def get_merge_requests_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集 Code Review (Merge Request) 資料"""
        print("\n正在收集 Code Review 資料...")
        mr_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                mrs = self.client.get_project_merge_requests(
                    project.id,
                    updated_after=self.start_date.isoformat(),
                    updated_before=self.end_date.isoformat()
                )
                
                for mr in mrs:
                    try:
                        mr_detail = self.client.get_merge_request_detail(project.id, mr.iid)
                        
                        # 取得討論/評論
                        discussions = self.client.get_merge_request_discussions(project.id, mr.iid)
                        comment_count = sum(len(d.attributes.get('notes', [])) for d in discussions)
                        
                        # 取得 approvals
                        approvals = getattr(mr_detail, 'approvals', None)
                        approved_by = []
                        if approvals:
                            approved_by = [a['user']['name'] for a in approvals.approved_by] if hasattr(approvals, 'approved_by') else []
                        
                        mr_data.append({
                            'project_id': project.id,
                            'project_name': project.name,
                            'mr_iid': mr.iid,
                            'mr_id': mr.id,
                            'title': mr.title,
                            'description': mr.description[:500] if mr.description else '',
                            'author_name': mr.author['name'],
                            'author_username': mr.author['username'],
                            'state': mr.state,
                            'merged': getattr(mr, 'merged_at', None) is not None,
                            'created_at': mr.created_at,
                            'updated_at': mr.updated_at,
                            'merged_at': getattr(mr, 'merged_at', None),
                            'merged_by': mr.merged_by['name'] if getattr(mr, 'merged_by', None) else '',
                            'source_branch': mr.source_branch,
                            'target_branch': mr.target_branch,
                            'upvotes': mr.upvotes,
                            'downvotes': mr.downvotes,
                            'comment_count': comment_count,
                            'changes_count': getattr(mr, 'changes_count', 0),
                            'approved_by': ','.join(approved_by),
                            'web_url': mr.web_url
                        })
                    except Exception as e:
                        continue
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        df = pd.DataFrame(mr_data)
        output_file = self.save_dataframe(df, "all-user.merge-requests.csv")
        print(f"✓ Code Review 資料已儲存至: {output_file}")
        return df
    
    def get_statistics_data(self, commits_df: pd.DataFrame, mr_df: pd.DataFrame) -> pd.DataFrame:
        """統計每位開發者的資料"""
        print("\n正在計算統計資料...")
        
        # Commit 統計
        commit_stats = commits_df.groupby('author_email').agg({
            'commit_id': 'count',
            'additions': 'sum',
            'deletions': 'sum',
            'total_changes': 'sum',
            'project_id': 'nunique',
            'author_name': 'first'
        }).rename(columns={
            'commit_id': 'total_commits',
            'additions': 'total_additions',
            'deletions': 'total_deletions',
            'total_changes': 'total_code_changes',
            'project_id': 'projects_contributed',
            'author_name': 'developer_name'
        })
        
        # MR 統計 (作為作者)
        mr_stats = mr_df.groupby('author_username').agg({
            'mr_id': 'count',
            'merged': 'sum',
            'comment_count': 'sum',
            'changes_count': 'sum'
        }).rename(columns={
            'mr_id': 'total_mrs_created',
            'merged': 'total_mrs_merged',
            'comment_count': 'total_mr_comments',
            'changes_count': 'total_mr_changes'
        })
        
        # 合併統計資料
        stats_df = commit_stats.reset_index()
        stats_df['avg_changes_per_commit'] = stats_df['total_code_changes'] / stats_df['total_commits']
        
        output_file = self.save_dataframe(stats_df, "all-user.statistics.csv")
        print(f"✓ 統計資料已儲存至: {output_file}")
        return stats_df
    
    # ==================== 新增的查詢方法 ====================
    
    def get_projects_list(self) -> List[Dict[str, Any]]:
        """取得所有專案列表（供外部呼叫）
        
        Returns:
            專案列表，每個專案包含: id, name, description, path, web_url, default_branch, 
            created_at, last_activity_at, star_count, forks_count
        """
        projects = self.get_all_projects()
        
        projects_info = []
        for project in projects:
            projects_info.append({
                'id': project.id,
                'name': project.name,
                'description': getattr(project, 'description', ''),
                'path': project.path,
                'path_with_namespace': project.path_with_namespace,
                'web_url': project.web_url,
                'default_branch': getattr(project, 'default_branch', ''),
                'created_at': project.created_at,
                'last_activity_at': getattr(project, 'last_activity_at', ''),
                'star_count': getattr(project, 'star_count', 0),
                'forks_count': getattr(project, 'forks_count', 0),
                'open_issues_count': getattr(project, 'open_issues_count', 0),
                'visibility': getattr(project, 'visibility', ''),
                'creator_id': getattr(project, 'creator_id', '')
            })
        
        return projects_info
    
    def get_all_users(self) -> List[Dict[str, Any]]:
        """取得所有使用者/帳號列表（供外部呼叫）
        
        Returns:
            使用者列表，每個使用者包含: id, username, name, email, state, avatar_url, 
            web_url, created_at, is_admin
        """
        print("正在取得使用者列表...")
        users = self.client.get_all_users()
        
        users_info = []
        for user in users:
            users_info.append({
                'id': user.id,
                'username': user.username,
                'name': user.name,
                'email': getattr(user, 'email', ''),
                'state': user.state,
                'avatar_url': getattr(user, 'avatar_url', ''),
                'web_url': user.web_url,
                'created_at': getattr(user, 'created_at', ''),
                'bio': getattr(user, 'bio', ''),
                'location': getattr(user, 'location', ''),
                'public_email': getattr(user, 'public_email', ''),
                'organization': getattr(user, 'organization', ''),
                'is_admin': getattr(user, 'is_admin', False),
                'can_create_project': getattr(user, 'can_create_project', False),
                'last_sign_in_at': getattr(user, 'last_sign_in_at', ''),
                'confirmed_at': getattr(user, 'confirmed_at', ''),
                'last_activity_on': getattr(user, 'last_activity_on', '')
            })
        
        print(f"找到 {len(users_info)} 位使用者")
        return users_info
    
    def get_user_commits_in_project(self, project_id: int, user_email: str = None, 
                                    user_name: str = None) -> List[Dict[str, Any]]:
        """取得特定使用者在特定專案的 commit 記錄（供外部呼叫）
        
        Args:
            project_id: 專案 ID
            user_email: 使用者 email（二選一）
            user_name: 使用者名稱（二選一）
            
        Returns:
            commit 列表，包含詳細的 commit 資訊
        """
        print(f"正在取得專案 {project_id} 的 commit 記錄...")
        
        commits = self.client.get_project_commits(
            project_id,
            since=self.start_date.isoformat(),
            until=self.end_date.isoformat()
        )
        
        project = self.client.get_project(project_id)
        
        commits_data = []
        for commit in commits:
            # 過濾指定使用者
            if user_email and commit.author_email != user_email:
                continue
            if user_name and commit.author_name != user_name:
                continue
                
            commit_detail = self.client.get_commit_detail(project_id, commit.id)
            
            # 取得檔案變更列表
            diffs = self.client.get_commit_diff(project_id, commit.id)
            changed_files = [diff.get('new_path', diff.get('old_path', '')) for diff in diffs]
            
            commits_data.append({
                'project_id': project.id,
                'project_name': project.name,
                'commit_id': commit.id,
                'commit_short_id': commit.short_id,
                'author_name': commit.author_name,
                'author_email': commit.author_email,
                'committer_name': getattr(commit, 'committer_name', ''),
                'committer_email': getattr(commit, 'committer_email', ''),
                'authored_date': getattr(commit, 'authored_date', ''),
                'committed_date': commit.committed_date,
                'title': commit.title,
                'message': commit.message,
                'additions': commit_detail.stats.get('additions', 0),
                'deletions': commit_detail.stats.get('deletions', 0),
                'total_changes': commit_detail.stats.get('total', 0),
                'parent_ids': commit.parent_ids,
                'web_url': commit.web_url,
                'changed_files': changed_files,
                'changed_files_count': len(changed_files)
            })
        
        print(f"找到 {len(commits_data)} 筆 commits")
        return commits_data
    
    def get_user_code_changes_in_project(self, project_id: int, user_email: str = None,
                                         user_name: str = None) -> List[Dict[str, Any]]:
        """取得特定使用者在特定專案的程式碼異動詳情（供外部呼叫）
        
        Args:
            project_id: 專案 ID
            user_email: 使用者 email（二選一）
            user_name: 使用者名稱（二選一）
            
        Returns:
            程式碼異動列表，包含每個檔案的詳細變更
        """
        print(f"正在取得專案 {project_id} 的程式碼異動...")
        
        commits = self.client.get_project_commits(
            project_id,
            since=self.start_date.isoformat(),
            until=self.end_date.isoformat()
        )
        
        project = self.client.get_project(project_id)
        
        changes_data = []
        for commit in commits:
            # 過濾指定使用者
            if user_email and commit.author_email != user_email:
                continue
            if user_name and commit.author_name != user_name:
                continue
                
            try:
                diffs = self.client.get_commit_diff(project_id, commit.id)
                
                for diff in diffs:
                    # 計算新增和刪除的行數
                    diff_content = diff.get('diff', '')
                    added_lines = diff_content.count('\n+') if diff_content else 0
                    removed_lines = diff_content.count('\n-') if diff_content else 0
                    
                    changes_data.append({
                        'project_id': project.id,
                        'project_name': project.name,
                        'commit_id': commit.id,
                        'commit_short_id': commit.short_id,
                        'author_name': commit.author_name,
                        'author_email': commit.author_email,
                        'committed_date': commit.committed_date,
                        'commit_title': commit.title,
                        'file_path': diff.get('new_path', diff.get('old_path', '')),
                        'old_path': diff.get('old_path', ''),
                        'new_path': diff.get('new_path', ''),
                        'new_file': diff.get('new_file', False),
                        'renamed_file': diff.get('renamed_file', False),
                        'deleted_file': diff.get('deleted_file', False),
                        'a_mode': diff.get('a_mode', ''),
                        'b_mode': diff.get('b_mode', ''),
                        'added_lines': added_lines,
                        'removed_lines': removed_lines,
                        'diff_content': diff_content[:5000],  # 保留更多內容
                        'web_url': commit.web_url
                    })
            except Exception as e:
                print(f"處理 commit {commit.id} 時發生錯誤: {str(e)}")
                continue
        
        print(f"找到 {len(changes_data)} 筆程式碼異動")
        return changes_data
    
    def get_user_merge_requests_in_project(self, project_id: int, user_username: str = None,
                                          user_name: str = None) -> List[Dict[str, Any]]:
        """取得特定使用者在特定專案的 Code Review (MR) 資訊（供外部呼叫）
        
        Args:
            project_id: 專案 ID
            user_username: 使用者 username（二選一）
            user_name: 使用者名稱（二選一）
            
        Returns:
            MR 列表，包含詳細的 code review 資訊
        """
        print(f"正在取得專案 {project_id} 的 Merge Request 記錄...")
        
        project = self.gl.projects.get(project_id)
        mrs = project.mergerequests.list(
            all=True,
            updated_after=self.start_date.isoformat(),
            updated_before=self.end_date.isoformat()
        )
        
        mr_data = []
        for mr in mrs:
            # 過濾指定使用者（作為作者）
            if user_username and mr.author['username'] != user_username:
                continue
            if user_name and mr.author['name'] != user_name:
                continue
                
            try:
                mr_detail = project.mergerequests.get(mr.iid)
                
                # 取得討論/評論
                discussions = mr_detail.discussions.list(all=True)
                all_notes = []
                for discussion in discussions:
                    notes = discussion.attributes.get('notes', [])
                    for note in notes:
                        all_notes.append({
                            'author': note.get('author', {}).get('name', ''),
                            'body': note.get('body', ''),
                            'created_at': note.get('created_at', ''),
                            'updated_at': note.get('updated_at', ''),
                            'resolvable': note.get('resolvable', False),
                            'resolved': note.get('resolved', False)
                        })
                
                # 取得 approvals
                approvals = getattr(mr_detail, 'approvals', None)
                approved_by = []
                if approvals:
                    approved_by = [a['user']['name'] for a in approvals.approved_by] if hasattr(approvals, 'approved_by') else []
                
                # 取得變更的檔案
                try:
                    changes = mr_detail.changes()
                    changed_files = [change['new_path'] for change in changes.get('changes', [])]
                except:
                    changed_files = []
                
                # 取得相關的 commits
                mr_commits = mr_detail.commits.list(all=True)
                commit_shas = [c.id for c in mr_commits]
                
                mr_data.append({
                    'project_id': project.id,
                    'project_name': project.name,
                    'mr_iid': mr.iid,
                    'mr_id': mr.id,
                    'title': mr.title,
                    'description': mr.description if mr.description else '',
                    'author_name': mr.author['name'],
                    'author_username': mr.author['username'],
                    'state': mr.state,
                    'merged': getattr(mr, 'merged_at', None) is not None,
                    'created_at': mr.created_at,
                    'updated_at': mr.updated_at,
                    'merged_at': getattr(mr, 'merged_at', None),
                    'closed_at': getattr(mr, 'closed_at', None),
                    'merged_by': mr.merged_by['name'] if getattr(mr, 'merged_by', None) else '',
                    'closed_by': mr.closed_by['name'] if getattr(mr, 'closed_by', None) else '',
                    'assignees': [a['name'] for a in getattr(mr, 'assignees', [])],
                    'reviewers': [r['name'] for r in getattr(mr, 'reviewers', [])],
                    'source_branch': mr.source_branch,
                    'target_branch': mr.target_branch,
                    'source_project_id': getattr(mr, 'source_project_id', ''),
                    'target_project_id': getattr(mr, 'target_project_id', ''),
                    'work_in_progress': getattr(mr, 'work_in_progress', False),
                    'draft': getattr(mr, 'draft', False),
                    'upvotes': mr.upvotes,
                    'downvotes': mr.downvotes,
                    'comment_count': len(all_notes),
                    'user_notes_count': getattr(mr, 'user_notes_count', 0),
                    'changes_count': getattr(mr, 'changes_count', 0),
                    'approved_by': approved_by,
                    'has_conflicts': getattr(mr, 'has_conflicts', False),
                    'blocking_discussions_resolved': getattr(mr, 'blocking_discussions_resolved', True),
                    'web_url': mr.web_url,
                    'changed_files': changed_files,
                    'changed_files_count': len(changed_files),
                    'commits': commit_shas,
                    'commits_count': len(commit_shas),
                    'discussions': all_notes,
                    'time_stats': {
                        'time_estimate': getattr(mr, 'time_estimate', 0),
                        'total_time_spent': getattr(mr, 'total_time_spent', 0),
                        'human_time_estimate': getattr(mr, 'human_time_estimate', ''),
                        'human_total_time_spent': getattr(mr, 'human_total_time_spent', '')
                    }
                })
            except Exception as e:
                print(f"處理 MR {mr.iid} 時發生錯誤: {str(e)}")
                continue
        
        print(f"找到 {len(mr_data)} 筆 Merge Requests")
        return mr_data
    
    def get_user_statistics_in_project(self, project_id: int, user_email: str = None,
                                       user_name: str = None, user_username: str = None) -> Dict[str, Any]:
        """取得特定使用者在特定專案的統計資訊（供外部呼叫）
        
        Args:
            project_id: 專案 ID
            user_email: 使用者 email
            user_name: 使用者名稱
            user_username: 使用者 username
            
        Returns:
            統計資訊字典，包含各項詳細數據
        """
        print(f"正在計算使用者在專案 {project_id} 的統計資訊...")
        
        # 取得各項資料
        commits = self.get_user_commits_in_project(project_id, user_email, user_name)
        code_changes = self.get_user_code_changes_in_project(project_id, user_email, user_name)
        mrs = self.get_user_merge_requests_in_project(project_id, user_username, user_name)
        
        # 計算統計資料
        total_commits = len(commits)
        total_additions = sum(c['additions'] for c in commits)
        total_deletions = sum(c['deletions'] for c in commits)
        total_changes = sum(c['total_changes'] for c in commits)
        
        # 檔案統計
        all_files = set()
        file_changes_count = {}
        for change in code_changes:
            file_path = change['file_path']
            all_files.add(file_path)
            file_changes_count[file_path] = file_changes_count.get(file_path, 0) + 1
        
        # 檔案類型統計
        file_types = {}
        for file_path in all_files:
            ext = os.path.splitext(file_path)[1] or 'no_extension'
            file_types[ext] = file_types.get(ext, 0) + 1
        
        # MR 統計
        total_mrs = len(mrs)
        merged_mrs = sum(1 for mr in mrs if mr['merged'])
        closed_mrs = sum(1 for mr in mrs if mr['state'] == 'closed')
        open_mrs = sum(1 for mr in mrs if mr['state'] == 'opened')
        total_mr_comments = sum(mr['comment_count'] for mr in mrs)
        
        # 時間統計
        if commits:
            commit_dates = [datetime.fromisoformat(c['committed_date'].replace('Z', '+00:00')) for c in commits]
            first_commit_date = min(commit_dates).isoformat()
            last_commit_date = max(commit_dates).isoformat()
            active_days = len(set(d.date() for d in commit_dates))
        else:
            first_commit_date = None
            last_commit_date = None
            active_days = 0
        
        statistics = {
            'project_id': project_id,
            'user_email': user_email,
            'user_name': user_name,
            'user_username': user_username,
            'period': {
                'start_date': self.start_date.isoformat(),
                'end_date': self.end_date.isoformat(),
                'first_commit_date': first_commit_date,
                'last_commit_date': last_commit_date,
                'active_days': active_days
            },
            'commits': {
                'total_commits': total_commits,
                'total_additions': total_additions,
                'total_deletions': total_deletions,
                'total_changes': total_changes,
                'avg_additions_per_commit': total_additions / total_commits if total_commits > 0 else 0,
                'avg_deletions_per_commit': total_deletions / total_commits if total_commits > 0 else 0,
                'avg_changes_per_commit': total_changes / total_commits if total_commits > 0 else 0
            },
            'files': {
                'total_files_changed': len(all_files),
                'total_file_changes': len(code_changes),
                'most_changed_files': sorted(file_changes_count.items(), key=lambda x: x[1], reverse=True)[:10],
                'file_types_distribution': file_types,
                'new_files_created': sum(1 for c in code_changes if c['new_file']),
                'files_deleted': sum(1 for c in code_changes if c['deleted_file']),
                'files_renamed': sum(1 for c in code_changes if c['renamed_file'])
            },
            'merge_requests': {
                'total_mrs_created': total_mrs,
                'merged_mrs': merged_mrs,
                'closed_mrs': closed_mrs,
                'open_mrs': open_mrs,
                'merge_rate': merged_mrs / total_mrs if total_mrs > 0 else 0,
                'total_mr_comments': total_mr_comments,
                'avg_comments_per_mr': total_mr_comments / total_mrs if total_mrs > 0 else 0
            },
            'productivity': {
                'commits_per_day': total_commits / active_days if active_days > 0 else 0,
                'changes_per_day': total_changes / active_days if active_days > 0 else 0,
                'files_per_commit': len(all_files) / total_commits if total_commits > 0 else 0
            }
        }
        
        return statistics

def main():
    """主程式"""
    print("=" * 60)
    print("GitLab 開發者程式碼品質分析工具")
    print("=" * 60)
    
    try:
        collector = GitLabCollector()
        
        # 1. 取得所有專案
        projects = collector.get_all_projects()
        
        # 2. 收集 Commit 資料
        commits_df = collector.get_commits_data(projects)
        
        # 3. 收集程式碼異動資料
        changes_df = collector.get_code_changes_data(projects)
        
        # 4. 收集 Code Review 資料
        mr_df = collector.get_merge_requests_data(projects)
        
        # 5. 產生統計資料
        stats_df = collector.get_statistics_data(commits_df, mr_df)
        
        print("\n" + "=" * 60)
        print("✓ 資料收集完成！")
        print(f"總共收集了 {len(commits_df)} 筆 commits")
        print(f"總共收集了 {len(changes_df)} 筆程式碼異動")
        print(f"總共收集了 {len(mr_df)} 筆 merge requests")
        print(f"總共分析了 {len(stats_df)} 位開發者")
        print("=" * 60)
        
    except Exception as e:
        print(f"\n錯誤: {str(e)}")
        raise

if __name__ == "__main__":
    main()
