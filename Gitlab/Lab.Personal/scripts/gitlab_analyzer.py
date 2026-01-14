"""
GitLab 資料收集器 (重構版)

統一的資料收集器，使用策略模式支援：
1. 收集所有開發者的資料
2. 收集特定開發者的資料
3. 提供程式化查詢 API

遵循 SOLID 原則：
- Single Responsibility: 各個方法職責單一
- Open/Closed: 透過策略模式擴展功能
- Liskov Substitution: FilterStrategy 可替換
- Interface Segregation: 清晰的方法介面
- Dependency Inversion: 依賴抽象的 FilterStrategy
"""

import pandas as pd
from datetime import datetime
from typing import List, Dict, Any, Optional
import os
from tqdm import tqdm
import config
from filters import FilterStrategy, AllDevelopersFilter, SpecificDeveloperFilter
from gitlab_client import GitLabClient


class GitLabCollector:
    """GitLab 資料收集器"""
    
    def __init__(
        self, 
        filter_strategy: Optional[FilterStrategy] = None,
        start_date: str = None, 
        end_date: str = None
    ):
        """
        初始化 GitLab 收集器
        
        Args:
            filter_strategy: 過濾策略（None 表示收集所有開發者）
            start_date: 起始日期 (格式: YYYY-MM-DD)
            end_date: 結束日期 (格式: YYYY-MM-DD)
        """
        self.client = GitLabClient(
            config.GITLAB_URL, 
            config.GITLAB_TOKEN, 
            ssl_verify=False
        )
        self.start_date = datetime.strptime(start_date or config.START_DATE, "%Y-%m-%d")
        self.end_date = datetime.strptime(end_date or config.END_DATE, "%Y-%m-%d")
        self.filter_strategy = filter_strategy or AllDevelopersFilter()
        
        # 確保輸出目錄存在
        os.makedirs(config.OUTPUT_DIR, exist_ok=True)
    
    # ==================== 專案與使用者查詢 ====================
    
    def get_all_projects(self) -> List[Any]:
        """取得所有專案（內部使用）"""
        print("正在取得專案列表...")
        projects = self.client.get_projects(
            group_id=config.TARGET_GROUP_ID,
            project_ids=config.TARGET_PROJECT_IDS
        )
        print(f"找到 {len(projects)} 個專案")
        return projects
    
    def get_projects_list(self) -> List[Dict[str, Any]]:
        """取得所有專案列表（供外部呼叫）"""
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
        """取得所有使用者列表"""
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
    
    # ==================== 資料收集方法 ====================
    
    def collect_commits(self, projects: List[Any], save_to_file: bool = True) -> pd.DataFrame:
        """
        收集 commit 資料
        
        Args:
            projects: 專案列表
            save_to_file: 是否儲存為 CSV 檔案
            
        Returns:
            DataFrame 包含所有 commit 資料
        """
        print(f"\n正在收集 Commit 資料...")
        commits_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                commits = self.client.get_project_commits(
                    project.id,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    # 使用策略模式過濾
                    if not self.filter_strategy.should_include_commit(commit):
                        continue
                    
                    try:
                        commit_detail = self.client.get_commit_detail(project.id, commit.id)
                        
                        # 取得變更的檔案列表
                        diffs = self.client.get_commit_diff(project.id, commit.id)
                        changed_files = [
                            diff.get('new_path', diff.get('old_path', '')) 
                            for diff in diffs
                        ]
                        
                        commits_data.append({
                            'project_id': project.id,
                            'project_name': project.name,
                            'project_path': project.path_with_namespace,
                            'commit_id': commit.id,
                            'commit_short_id': commit.short_id,
                            'author_name': commit.author_name,
                            'author_email': commit.author_email,
                            'committer_name': getattr(commit, 'committer_name', ''),
                            'committer_email': getattr(commit, 'committer_email', ''),
                            'authored_date': getattr(commit, 'authored_date', ''),
                            'committed_date': commit.committed_date,
                            'created_at': getattr(commit, 'created_at', ''),
                            'title': commit.title,
                            'message': commit.message,
                            'additions': commit_detail.stats.get('additions', 0),
                            'deletions': commit_detail.stats.get('deletions', 0),
                            'total_changes': commit_detail.stats.get('total', 0),
                            'parent_ids': ','.join(commit.parent_ids) if commit.parent_ids else '',
                            'web_url': commit.web_url,
                            'changed_files': ','.join(changed_files),
                            'changed_files_count': len(changed_files)
                        })
                    except Exception as e:
                        continue
                        
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        df = pd.DataFrame(commits_data)
        
        if save_to_file:
            identifier = self.filter_strategy.get_identifier()
            output_file = os.path.join(config.OUTPUT_DIR, f"{identifier}.commits.csv")
            df.to_csv(output_file, index=False, encoding='utf-8-sig')
            print(f"✓ Commit 資料已儲存至: {output_file}")
            print(f"  共收集 {len(df)} 筆 commits")
        
        return df
    
    def collect_code_changes(self, projects: List[Any], save_to_file: bool = True) -> pd.DataFrame:
        """
        收集程式碼異動資料
        
        Args:
            projects: 專案列表
            save_to_file: 是否儲存為 CSV 檔案
            
        Returns:
            DataFrame 包含所有程式碼異動資料
        """
        print(f"\n正在收集程式碼異動資料...")
        changes_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                commits = self.client.get_project_commits(
                    project.id,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    # 使用策略模式過濾
                    if not self.filter_strategy.should_include_commit(commit):
                        continue
                    
                    try:
                        diffs = self.client.get_commit_diff(project.id, commit.id)
                        
                        for diff in diffs:
                            file_path = diff.get('new_path', diff.get('old_path', ''))
                            file_extension = os.path.splitext(file_path)[1] if file_path else ''
                            
                            # 計算新增和刪除的行數
                            diff_content = diff.get('diff', '')
                            added_lines = diff_content.count('\n+') if diff_content else 0
                            removed_lines = diff_content.count('\n-') if diff_content else 0
                            
                            changes_data.append({
                                'project_id': project.id,
                                'project_name': project.name,
                                'project_path': project.path_with_namespace,
                                'commit_id': commit.id,
                                'commit_short_id': commit.short_id,
                                'author_name': commit.author_name,
                                'author_email': commit.author_email,
                                'committed_date': commit.committed_date,
                                'commit_title': commit.title,
                                'file_path': file_path,
                                'file_extension': file_extension,
                                'old_path': diff.get('old_path', ''),
                                'new_path': diff.get('new_path', ''),
                                'new_file': diff.get('new_file', False),
                                'renamed_file': diff.get('renamed_file', False),
                                'deleted_file': diff.get('deleted_file', False),
                                'a_mode': diff.get('a_mode', ''),
                                'b_mode': diff.get('b_mode', ''),
                                'added_lines': added_lines,
                                'removed_lines': removed_lines,
                                'diff_content': diff_content[:5000],
                                'web_url': commit.web_url
                            })
                    except Exception as e:
                        continue
                        
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        df = pd.DataFrame(changes_data)
        
        if save_to_file:
            identifier = self.filter_strategy.get_identifier()
            output_file = os.path.join(config.OUTPUT_DIR, f"{identifier}.code-changes.csv")
            df.to_csv(output_file, index=False, encoding='utf-8-sig')
            print(f"✓ 程式碼異動資料已儲存至: {output_file}")
            print(f"  共收集 {len(df)} 筆程式碼異動")
        
        return df
    
    def collect_merge_requests(self, projects: List[Any], save_to_file: bool = True) -> tuple:
        """
        收集 Merge Request 資料
        
        Args:
            projects: 專案列表
            save_to_file: 是否儲存為 CSV 檔案
            
        Returns:
            (作者的 MR DataFrame, 參與審查的 MR DataFrame)
        """
        print(f"\n正在收集 Merge Request 資料...")
        mr_data = []
        review_data = []
        
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
                        
                        # 判斷是否為目標作者
                        is_author = self.filter_strategy.should_include_merge_request(mr)
                        
                        # 取得討論/評論
                        discussions = self.client.get_merge_request_discussions(project.id, mr.iid)
                        all_notes = []
                        participated_in_review = False
                        
                        for discussion in discussions:
                            notes = discussion.attributes.get('notes', [])
                            for note in notes:
                                note_author = note.get('author', {})
                                all_notes.append({
                                    'author': note_author.get('name', ''),
                                    'body': note.get('body', ''),
                                    'created_at': note.get('created_at', ''),
                                    'updated_at': note.get('updated_at', ''),
                                    'resolvable': note.get('resolvable', False),
                                    'resolved': note.get('resolved', False)
                                })
                                
                                # 檢查是否參與審查
                                if not is_author and self.filter_strategy.should_include_review(note_author):
                                    participated_in_review = True
                        
                        comment_count = len(all_notes)
                        
                        # 取得 approvals
                        approvals = getattr(mr_detail, 'approvals', None)
                        approved_by = []
                        if approvals and hasattr(approvals, 'approved_by'):
                            approved_by = [a['user']['name'] for a in approvals.approved_by]
                        
                        # 取得變更的檔案
                        try:
                            changes = self.client.get_merge_request_changes(project.id, mr.iid)
                            changed_files = [
                                change['new_path'] 
                                for change in changes.get('changes', [])
                            ]
                        except:
                            changed_files = []
                        
                        # 取得相關的 commits
                        try:
                            mr_commits = mr_detail.commits.list(all=True)
                            commit_shas = [c.id for c in mr_commits]
                        except:
                            commit_shas = []
                        
                        mr_record = {
                            'project_id': project.id,
                            'project_name': project.name,
                            'project_path': project.path_with_namespace,
                            'mr_iid': mr.iid,
                            'mr_id': mr.id,
                            'title': mr.title,
                            'description': (mr.description[:1000] if mr.description else ''),
                            'author_name': mr.author['name'],
                            'author_username': mr.author['username'],
                            'author_email': mr.author.get('email', ''),
                            'state': mr.state,
                            'merged': getattr(mr, 'merged_at', None) is not None,
                            'created_at': mr.created_at,
                            'updated_at': mr.updated_at,
                            'merged_at': getattr(mr, 'merged_at', None),
                            'closed_at': getattr(mr, 'closed_at', None),
                            'merged_by': (mr.merged_by['name'] if getattr(mr, 'merged_by', None) else ''),
                            'closed_by': (mr.closed_by['name'] if getattr(mr, 'closed_by', None) else ''),
                            'assignees': ','.join([a['name'] for a in getattr(mr, 'assignees', [])]),
                            'reviewers': ','.join([r['name'] for r in getattr(mr, 'reviewers', [])]),
                            'source_branch': mr.source_branch,
                            'target_branch': mr.target_branch,
                            'source_project_id': getattr(mr, 'source_project_id', ''),
                            'target_project_id': getattr(mr, 'target_project_id', ''),
                            'work_in_progress': getattr(mr, 'work_in_progress', False),
                            'draft': getattr(mr, 'draft', False),
                            'upvotes': mr.upvotes,
                            'downvotes': mr.downvotes,
                            'comment_count': comment_count,
                            'user_notes_count': getattr(mr, 'user_notes_count', 0),
                            'changes_count': getattr(mr, 'changes_count', 0),
                            'approved_by': ','.join(approved_by),
                            'has_conflicts': getattr(mr, 'has_conflicts', False),
                            'blocking_discussions_resolved': getattr(mr, 'blocking_discussions_resolved', True),
                            'labels': ','.join(mr.labels) if mr.labels else '',
                            'milestone': (mr.milestone['title'] if getattr(mr, 'milestone', None) else ''),
                            'web_url': mr.web_url,
                            'changed_files_count': len(changed_files),
                            'commits_count': len(commit_shas),
                            'comments_detail': str(all_notes)[:2000] if all_notes else ''
                        }
                        
                        if is_author:
                            mr_data.append(mr_record)
                        
                        if participated_in_review:
                            review_data.append(mr_record)
                            
                    except Exception as e:
                        continue
                        
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        mr_df = pd.DataFrame(mr_data)
        review_df = pd.DataFrame(review_data)
        
        if save_to_file:
            identifier = self.filter_strategy.get_identifier()
            
            # 儲存作者的 MR
            mr_output = os.path.join(config.OUTPUT_DIR, f"{identifier}.merge-requests.csv")
            mr_df.to_csv(mr_output, index=False, encoding='utf-8-sig')
            print(f"✓ Merge Request 資料已儲存至: {mr_output}")
            print(f"  共收集 {len(mr_df)} 筆 MRs")
            
            # 儲存參與審查的 MR（僅針對特定開發者）
            if not isinstance(self.filter_strategy, AllDevelopersFilter) and len(review_df) > 0:
                review_output = os.path.join(config.OUTPUT_DIR, f"{identifier}.code-reviews.csv")
                review_df.to_csv(review_output, index=False, encoding='utf-8-sig')
                print(f"✓ Code Review 參與記錄已儲存至: {review_output}")
                print(f"  共收集 {len(review_df)} 筆 Reviews")
        
        return mr_df, review_df
    
    def calculate_statistics(
        self, 
        commits_df: pd.DataFrame, 
        mr_df: pd.DataFrame,
        save_to_file: bool = True
    ) -> pd.DataFrame:
        """
        計算統計資料
        
        Args:
            commits_df: Commit DataFrame
            mr_df: Merge Request DataFrame
            save_to_file: 是否儲存為 CSV 檔案
            
        Returns:
            統計資料 DataFrame
        """
        print("\n正在計算統計資料...")
        
        if len(commits_df) == 0:
            print("沒有 commit 資料，無法計算統計")
            return pd.DataFrame()
        
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
        
        # MR 統計（如果有 MR 資料）
        if len(mr_df) > 0:
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
        stats_df['avg_changes_per_commit'] = (
            stats_df['total_code_changes'] / stats_df['total_commits']
        )
        
        if save_to_file:
            identifier = self.filter_strategy.get_identifier()
            output_file = os.path.join(config.OUTPUT_DIR, f"{identifier}.statistics.csv")
            stats_df.to_csv(output_file, index=False, encoding='utf-8-sig')
            print(f"✓ 統計資料已儲存至: {output_file}")
        
        return stats_df
    
    # ==================== 程式化查詢 API ====================
    
    def get_user_commits_in_project(
        self,
        project_id: int,
        user_email: str = None,
        user_name: str = None
    ) -> List[Dict[str, Any]]:
        """取得特定使用者在特定專案的 commit 記錄"""
        print(f"正在取得專案 {project_id} 的 commit 記錄...")
        
        # 建立臨時過濾器
        temp_filter = SpecificDeveloperFilter(email=user_email, name=user_name)
        original_filter = self.filter_strategy
        self.filter_strategy = temp_filter
        
        try:
            project = self.client.get_project(project_id)
            df = self.collect_commits([project], save_to_file=False)
            return df.to_dict('records')
        finally:
            self.filter_strategy = original_filter
    
    def get_user_code_changes_in_project(
        self,
        project_id: int,
        user_email: str = None,
        user_name: str = None
    ) -> List[Dict[str, Any]]:
        """取得特定使用者在特定專案的程式碼異動"""
        print(f"正在取得專案 {project_id} 的程式碼異動...")
        
        temp_filter = SpecificDeveloperFilter(email=user_email, name=user_name)
        original_filter = self.filter_strategy
        self.filter_strategy = temp_filter
        
        try:
            project = self.client.get_project(project_id)
            df = self.collect_code_changes([project], save_to_file=False)
            return df.to_dict('records')
        finally:
            self.filter_strategy = original_filter
    
    def get_user_merge_requests_in_project(
        self,
        project_id: int,
        user_username: str = None,
        user_name: str = None
    ) -> List[Dict[str, Any]]:
        """取得特定使用者在特定專案的 MR 記錄"""
        print(f"正在取得專案 {project_id} 的 MR 記錄...")
        
        temp_filter = SpecificDeveloperFilter(username=user_username, name=user_name)
        original_filter = self.filter_strategy
        self.filter_strategy = temp_filter
        
        try:
            project = self.client.get_project(project_id)
            mr_df, _ = self.collect_merge_requests([project], save_to_file=False)
            return mr_df.to_dict('records')
        finally:
            self.filter_strategy = original_filter
    
    def get_user_statistics_in_project(
        self,
        project_id: int,
        user_email: str = None,
        user_name: str = None,
        user_username: str = None
    ) -> Dict[str, Any]:
        """取得特定使用者在特定專案的統計資訊"""
        print(f"正在計算專案 {project_id} 的統計資訊...")
        
        # 收集資料
        commits = self.get_user_commits_in_project(project_id, user_email, user_name)
        code_changes = self.get_user_code_changes_in_project(project_id, user_email, user_name)
        mrs = self.get_user_merge_requests_in_project(project_id, user_username, user_name)
        
        # 計算統計
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
        
        # 時間統計
        if commits:
            commit_dates = [
                datetime.fromisoformat(c['committed_date'].replace('Z', '+00:00')) 
                for c in commits
            ]
            first_commit_date = min(commit_dates).isoformat()
            last_commit_date = max(commit_dates).isoformat()
            active_days = len(set(d.date() for d in commit_dates))
        else:
            first_commit_date = None
            last_commit_date = None
            active_days = 0
        
        return {
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
                'most_changed_files': sorted(
                    file_changes_count.items(), 
                    key=lambda x: x[1], 
                    reverse=True
                )[:10],
                'file_types_distribution': file_types,
                'new_files_created': sum(1 for c in code_changes if c['new_file']),
                'files_deleted': sum(1 for c in code_changes if c['deleted_file']),
                'files_renamed': sum(1 for c in code_changes if c['renamed_file'])
            },
            'merge_requests': {
                'total_mrs_created': total_mrs,
                'merged_mrs': merged_mrs,
                'merge_rate': merged_mrs / total_mrs if total_mrs > 0 else 0
            },
            'productivity': {
                'commits_per_day': total_commits / active_days if active_days > 0 else 0,
                'changes_per_day': total_changes / active_days if active_days > 0 else 0
            }
        }


def main():
    """主程式"""
    import sys
    
    print("=" * 60)
    print("GitLab 開發者程式碼品質分析工具 (重構版)")
    print("=" * 60)
    
    # 判斷執行模式
    if len(sys.argv) > 1:
        # 特定開發者模式
        identifier = sys.argv[1]
        
        # 判斷是 email 還是 username
        if '@' in identifier:
            filter_strategy = SpecificDeveloperFilter(email=identifier)
            print(f"\n分析開發者: {identifier} (by email)")
        else:
            filter_strategy = SpecificDeveloperFilter(username=identifier)
            print(f"\n分析開發者: {identifier} (by username)")
    else:
        # 全體開發者模式
        filter_strategy = AllDevelopersFilter()
        print("\n分析模式: 全體開發者")
    
    try:
        collector = GitLabCollector(filter_strategy=filter_strategy)
        
        # 1. 取得所有專案
        projects = collector.get_all_projects()
        projects = projects[:5]  # 只處理前 5 個專案
        
        # 2. 收集 Commit 資料
        commits_df = collector.collect_commits(projects)
        
        # 3. 收集程式碼異動資料
        changes_df = collector.collect_code_changes(projects)
        
        # 4. 收集 Code Review 資料
        mr_df, review_df = collector.collect_merge_requests(projects)
        
        # 5. 產生統計資料
        stats_df = collector.calculate_statistics(commits_df, mr_df)
        
        print("\n" + "=" * 60)
        print("✓ 資料收集完成！")
        print(f"總共收集了 {len(commits_df)} 筆 commits")
        print(f"總共收集了 {len(changes_df)} 筆程式碼異動")
        print(f"總共收集了 {len(mr_df)} 筆 merge requests")
        if len(review_df) > 0:
            print(f"總共收集了 {len(review_df)} 筆 code reviews")
        print(f"總共分析了 {len(stats_df)} 位開發者")
        print("=" * 60)
        
    except Exception as e:
        print(f"\n錯誤: {str(e)}")
        import traceback
        traceback.print_exc()
        raise


if __name__ == "__main__":
    main()
