"""
GitLab 特定開發者資料收集器 - 收集指定開發者的詳細程式碼資料
"""

import pandas as pd
from typing import List, Any, Optional
import os
from tqdm import tqdm
import config
import warnings
from base_gitlab_collector import BaseGitLabCollector

warnings.filterwarnings('ignore', category=Warning)

class GitLabDeveloperCollector(BaseGitLabCollector):
    def __init__(
        self, 
        developer_email: Optional[str] = None, 
        developer_username: Optional[str] = None,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None,
        project_ids: Optional[List[int]] = None,
        group_id: Optional[int] = None
    ):
        """
        初始化 GitLab 連線
        
        Args:
            developer_email: 開發者的 email (例如: user@example.com)
            developer_username: 開發者的 GitLab username (例如: johndoe)
            start_date: 起始日期 (格式: YYYY-MM-DD)，預設使用 config.START_DATE
            end_date: 結束日期 (格式: YYYY-MM-DD)，預設使用 config.END_DATE
            project_ids: 指定專案 ID 列表，預設使用 config.TARGET_PROJECT_IDS
            group_id: 指定群組 ID，預設使用 config.TARGET_GROUP_ID
        """
        super().__init__(start_date, end_date, project_ids, group_id)
        self.developer_email = developer_email
        self.developer_username = developer_username
        
        if not developer_email and not developer_username:
            raise ValueError("請至少提供 developer_email 或 developer_username")
    
    def _is_target_developer(self, commit) -> bool:
        """判斷是否為目標開發者的 commit"""
        if self.developer_email and commit.author_email == self.developer_email:
            return True
        if self.developer_username and hasattr(commit, 'author_name'):
            return self.developer_username.lower() in commit.author_name.lower()
        return False
    
    def get_commits_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集特定開發者的 commit 資料"""
        print(f"\n正在收集開發者 ({self.developer_email or self.developer_username}) 的 Commit 資料...")
        commits_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                commits = self.client.get_project_commits(
                    project.id,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    # 只收集目標開發者的 commit
                    if self.developer_email and commit.author_email != self.developer_email:
                        continue
                    
                    try:
                        commit_detail = self.client.get_commit_detail(project.id, commit.id)
                        
                        commits_data.append({
                            'project_id': project.id,
                            'project_name': project.name,
                            'project_path': project.path_with_namespace,
                            'commit_id': commit.id,
                            'commit_short_id': commit.short_id,
                            'author_name': commit.author_name,
                            'author_email': commit.author_email,
                            'committer_name': commit.committer_name,
                            'committer_email': commit.committer_email,
                            'committed_date': commit.committed_date,
                            'created_at': commit.created_at,
                            'title': commit.title,
                            'message': commit.message,
                            'additions': commit_detail.stats.get('additions', 0),
                            'deletions': commit_detail.stats.get('deletions', 0),
                            'total_changes': commit_detail.stats.get('total', 0),
                            'parent_ids': ','.join(commit.parent_ids) if commit.parent_ids else '',
                            'web_url': commit.web_url
                        })
                    except Exception as e:
                        continue
                        
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        df = pd.DataFrame(commits_data)
        identifier = self.developer_email or self.developer_username
        safe_identifier = identifier.replace('@', '_at_').replace('.', '_')
        output_file = self.save_dataframe(df, f"{safe_identifier}.commits.csv")
        print(f"✓ Commit 資料已儲存至: {output_file}")
        print(f"  共收集 {len(df)} 筆 commits")
        return df
    
    def get_code_changes_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集特定開發者的程式碼異動詳細資料"""
        print(f"\n正在收集開發者 ({self.developer_email or self.developer_username}) 的程式碼異動資料...")
        changes_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                commits = self.client.get_project_commits(
                    project.id,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    # 只收集目標開發者的 commit
                    if self.developer_email and commit.author_email != self.developer_email:
                        continue
                    
                    try:
                        diffs = self.client.get_commit_diff(project.id, commit.id)
                        
                        for diff in diffs:
                            # 取得檔案類型
                            file_path = diff.get('new_path', diff.get('old_path', ''))
                            file_extension = os.path.splitext(file_path)[1] if file_path else ''
                            
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
                                'diff_content': diff.get('diff', '')[:2000],  # 儲存前 2000 字元
                                'web_url': commit.web_url
                            })
                    except Exception as e:
                        continue
                        
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        df = pd.DataFrame(changes_data)
        identifier = self.developer_email or self.developer_username
        safe_identifier = identifier.replace('@', '_at_').replace('.', '_')
        output_file = self.save_dataframe(df, f"{safe_identifier}.code-changes.csv")
        print(f"✓ 程式碼異動資料已儲存至: {output_file}")
        print(f"  共收集 {len(df)} 筆程式碼異動")
        return df
    
    def get_merge_requests_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集特定開發者的 Code Review (Merge Request) 資料"""
        print(f"\n正在收集開發者 ({self.developer_email or self.developer_username}) 的 Code Review 資料...")
        mr_data = []
        mr_review_data = []
        
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
                        
                        # 判斷是否為目標開發者創建的 MR
                        is_author = False
                        if self.developer_email:
                            is_author = mr.author.get('email', '').lower() == self.developer_email.lower()
                        elif self.developer_username:
                            is_author = mr.author.get('username', '').lower() == self.developer_username.lower()
                        
                        # 取得討論/評論
                        discussions = self.client.get_merge_request_discussions(project.id, mr.iid)
                        comment_count = sum(len(d.attributes.get('notes', [])) for d in discussions)
                        
                        # 收集所有評論
                        comments = []
                        for discussion in discussions:
                            for note in discussion.attributes.get('notes', []):
                                comments.append({
                                    'note_id': note.get('id'),
                                    'author': note.get('author', {}).get('name', ''),
                                    'created_at': note.get('created_at', ''),
                                    'body': note.get('body', '')[:500]  # 限制長度
                                })
                        
                        # 取得 approvals
                        approvals = getattr(mr_detail, 'approvals', None)
                        approved_by = []
                        if approvals and hasattr(approvals, 'approved_by'):
                            approved_by = [a['user']['name'] for a in approvals.approved_by]
                        
                        # 取得變更的檔案
                        changes = self.client.get_merge_request_changes(project.id, mr.iid)
                        changed_files = []
                        if 'changes' in changes:
                            changed_files = [
                                {
                                    'old_path': c.get('old_path', ''),
                                    'new_path': c.get('new_path', ''),
                                    'new_file': c.get('new_file', False),
                                    'deleted_file': c.get('deleted_file', False),
                                    'renamed_file': c.get('renamed_file', False)
                                }
                                for c in changes['changes']
                            ]
                        
                        if is_author:
                            mr_data.append({
                                'project_id': project.id,
                                'project_name': project.name,
                                'project_path': project.path_with_namespace,
                                'mr_iid': mr.iid,
                                'mr_id': mr.id,
                                'title': mr.title,
                                'description': mr.description[:1000] if mr.description else '',
                                'author_name': mr.author['name'],
                                'author_username': mr.author['username'],
                                'author_email': mr.author.get('email', ''),
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
                                'labels': ','.join(mr.labels) if mr.labels else '',
                                'milestone': mr.milestone['title'] if getattr(mr, 'milestone', None) else '',
                                'assignees': ','.join([a['name'] for a in getattr(mr, 'assignees', [])]),
                                'reviewers': ','.join([r['name'] for r in getattr(mr, 'reviewers', [])]),
                                'has_conflicts': getattr(mr, 'has_conflicts', False),
                                'blocking_discussions_resolved': getattr(mr, 'blocking_discussions_resolved', True),
                                'web_url': mr.web_url,
                                'changed_files_count': len(changed_files),
                                'comments_detail': str(comments)[:2000] if comments else ''
                            })
                        
                        # 檢查是否有參與 Review (評論)
                        participated_in_review = False
                        for discussion in discussions:
                            for note in discussion.attributes.get('notes', []):
                                note_author_email = note.get('author', {}).get('email', '')
                                note_author_username = note.get('author', {}).get('username', '')
                                
                                if self.developer_email and note_author_email.lower() == self.developer_email.lower():
                                    participated_in_review = True
                                    break
                                elif self.developer_username and note_author_username.lower() == self.developer_username.lower():
                                    participated_in_review = True
                                    break
                            if participated_in_review:
                                break
                        
                        if participated_in_review and not is_author:
                            mr_review_data.append({
                                'project_id': project.id,
                                'project_name': project.name,
                                'mr_iid': mr.iid,
                                'mr_title': mr.title,
                                'mr_author': mr.author['name'],
                                'reviewed_at': mr.updated_at,
                                'mr_state': mr.state,
                                'web_url': mr.web_url
                            })
                            
                    except Exception as e:
                        continue
                        
            except Exception as e:
                print(f"處理專案 {project.name} 時發生錯誤: {str(e)}")
                continue
        
        identifier = self.developer_email or self.developer_username
        safe_identifier = identifier.replace('@', '_at_').replace('.', '_')
        
        # 儲存創建的 MR
        df_mr = pd.DataFrame(mr_data)
        output_file_mr = self.save_dataframe(df_mr, f"{safe_identifier}.merge-requests.csv")
        print(f"✓ Merge Request 資料已儲存至: {output_file_mr}")
        print(f"  共收集 {len(df_mr)} 筆 MR (作為作者)")
        
        # 儲存參與 Review 的 MR
        df_review = pd.DataFrame(mr_review_data)
        output_file_review = self.save_dataframe(df_review, f"{safe_identifier}.code-reviews.csv")
        print(f"✓ Code Review 參與資料已儲存至: {output_file_review}")
        print(f"  共收集 {len(df_review)} 筆 MR (參與 Review)")
        
        return df_mr
    
    def get_statistics_data(self, commits_df: pd.DataFrame, changes_df: pd.DataFrame, mr_df: pd.DataFrame) -> pd.DataFrame:
        """統計特定開發者的資料"""
        print("\n正在計算統計資料...")
        
        stats = {
            'developer_email': self.developer_email or 'N/A',
            'developer_username': self.developer_username or 'N/A',
            'developer_name': commits_df['author_name'].iloc[0] if len(commits_df) > 0 else 'N/A',
            'analysis_start_date': self.start_date.strftime('%Y-%m-%d'),
            'analysis_end_date': self.end_date.strftime('%Y-%m-%d'),
            
            # Commit 統計
            'total_commits': len(commits_df),
            'total_additions': commits_df['additions'].sum() if len(commits_df) > 0 else 0,
            'total_deletions': commits_df['deletions'].sum() if len(commits_df) > 0 else 0,
            'total_code_changes': commits_df['total_changes'].sum() if len(commits_df) > 0 else 0,
            'avg_changes_per_commit': commits_df['total_changes'].mean() if len(commits_df) > 0 else 0,
            'max_changes_in_commit': commits_df['total_changes'].max() if len(commits_df) > 0 else 0,
            'min_changes_in_commit': commits_df['total_changes'].min() if len(commits_df) > 0 else 0,
            
            # 專案統計
            'projects_contributed': commits_df['project_id'].nunique() if len(commits_df) > 0 else 0,
            'projects_list': ','.join(commits_df['project_name'].unique()[:10]) if len(commits_df) > 0 else '',
            
            # 程式碼異動統計
            'total_file_changes': len(changes_df),
            'new_files_created': changes_df['new_file'].sum() if len(changes_df) > 0 else 0,
            'files_deleted': changes_df['deleted_file'].sum() if len(changes_df) > 0 else 0,
            'files_renamed': changes_df['renamed_file'].sum() if len(changes_df) > 0 else 0,
            
            # 檔案類型統計
            'file_types': ','.join(changes_df['file_extension'].value_counts().head(10).index.tolist()) if len(changes_df) > 0 else '',
            'most_modified_file_type': changes_df['file_extension'].mode()[0] if len(changes_df) > 0 and not changes_df['file_extension'].mode().empty else '',
            
            # MR 統計
            'total_mrs_created': len(mr_df),
            'mrs_merged': mr_df['merged'].sum() if len(mr_df) > 0 else 0,
            'mrs_merge_rate': (mr_df['merged'].sum() / len(mr_df) * 100) if len(mr_df) > 0 else 0,
            'total_mr_comments_received': mr_df['comment_count'].sum() if len(mr_df) > 0 else 0,
            'avg_comments_per_mr': mr_df['comment_count'].mean() if len(mr_df) > 0 else 0,
            
            # 時間統計
            'first_commit_date': commits_df['committed_date'].min() if len(commits_df) > 0 else 'N/A',
            'last_commit_date': commits_df['committed_date'].max() if len(commits_df) > 0 else 'N/A',
        }
        
        df_stats = pd.DataFrame([stats])
        
        identifier = self.developer_email or self.developer_username
        safe_identifier = identifier.replace('@', '_at_').replace('.', '_')
        output_file = self.save_dataframe(df_stats, f"{safe_identifier}.statistics.csv")
        print(f"✓ 統計資料已儲存至: {output_file}")
        
        return df_stats
    
    def generate_summary_report(self, stats_df: pd.DataFrame) -> str:
        """產生摘要報告"""
        stats = stats_df.iloc[0]
        
        report = f"""
{'=' * 70}
GitLab 開發者分析報告
{'=' * 70}

開發者資訊:
  姓名: {stats['developer_name']}
  Email: {stats['developer_email']}
  Username: {stats['developer_username']}

分析期間:
  {stats['analysis_start_date']} 至 {stats['analysis_end_date']}

{'=' * 70}
Commit 統計:
  總 Commits: {stats['total_commits']:,}
  程式碼增加: {stats['total_additions']:,} 行
  程式碼刪除: {stats['total_deletions']:,} 行
  總變更量: {stats['total_code_changes']:,} 行
  平均每次變更: {stats['avg_changes_per_commit']:.1f} 行
  最大單次變更: {stats['max_changes_in_commit']:,} 行

{'=' * 70}
專案貢獻:
  參與專案數: {stats['projects_contributed']}
  
{'=' * 70}
程式碼異動:
  總檔案變更: {stats['total_file_changes']:,}
  新增檔案: {stats['new_files_created']}
  刪除檔案: {stats['files_deleted']}
  重新命名: {stats['files_renamed']}
  最常修改檔案類型: {stats['most_modified_file_type']}

{'=' * 70}
Code Review (Merge Requests):
  創建的 MR: {stats['total_mrs_created']}
  已合併 MR: {stats['mrs_merged']}
  合併率: {stats['mrs_merge_rate']:.1f}%
  收到評論數: {stats['total_mr_comments_received']}
  平均每個 MR 收到評論: {stats['avg_comments_per_mr']:.1f} 個

{'=' * 70}
活動時間:
  第一次 Commit: {stats['first_commit_date']}
  最後一次 Commit: {stats['last_commit_date']}

{'=' * 70}
"""
        return report

def main():
    """主程式"""
    import sys
    
    print("=" * 70)
    print("GitLab 特定開發者程式碼品質分析工具")
    print("=" * 70)
    
    # 從命令列參數或互動式輸入取得開發者資訊
    developer_email = None
    developer_username = None
    
    if len(sys.argv) > 1:
        if '@' in sys.argv[1]:
            developer_email = sys.argv[1]
        else:
            developer_username = sys.argv[1]
    else:
        print("\n請輸入開發者資訊 (擇一即可):")
        email_input = input("  Email (例如: user@example.com): ").strip()
        username_input = input("  Username (例如: johndoe): ").strip()
        
        if email_input:
            developer_email = email_input
        elif username_input:
            developer_username = username_input
        else:
            print("錯誤: 請至少提供 Email 或 Username")
            sys.exit(1)
    
    try:
        collector = GitLabDeveloperCollector(
            developer_email=developer_email,
            developer_username=developer_username
        )
        
        # 1. 取得所有專案
        projects = collector.get_all_projects()
        
        # 2. 收集 Commit 資料
        commits_df = collector.get_commits_data(projects)
        
        if len(commits_df) == 0:
            print("\n警告: 未找到此開發者的任何 commit 資料")
            print("請確認:")
            print("  1. Email 或 Username 是否正確")
            print("  2. 時間範圍設定是否涵蓋該開發者的活動期間")
            print("  3. 是否有權限存取相關專案")
            return
        
        # 3. 收集程式碼異動資料
        changes_df = collector.get_code_changes_data(projects)
        
        # 4. 收集 Code Review 資料
        mr_df = collector.get_merge_requests_data(projects)
        
        # 5. 產生統計資料
        stats_df = collector.get_statistics_data(commits_df, changes_df, mr_df)
        
        # 6. 產生並顯示摘要報告
        report = collector.generate_summary_report(stats_df)
        print(report)
        
        # 儲存報告
        identifier = developer_email or developer_username
        safe_identifier = identifier.replace('@', '_at_').replace('.', '_')
        report_file = os.path.join(config.OUTPUT_DIR, f"{safe_identifier}.report.txt")
        with open(report_file, 'w', encoding='utf-8') as f:
            f.write(report)
        print(f"✓ 報告已儲存至: {report_file}")
        
        print("\n" + "=" * 70)
        print("✓ 分析完成！")
        print("=" * 70)
        
    except Exception as e:
        print(f"\n錯誤: {str(e)}")
        import traceback
        traceback.print_exc()
        raise

if __name__ == "__main__":
    main()
