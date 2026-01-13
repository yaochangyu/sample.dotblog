"""
GitLab 資料收集器 - 收集所有開發者的程式碼品質資料
"""

import gitlab
import pandas as pd
from datetime import datetime
from typing import List, Dict, Any
import os
from tqdm import tqdm
import config

class GitLabCollector:
    def __init__(self):
        """初始化 GitLab 連線"""
        self.gl = gitlab.Gitlab(config.GITLAB_URL, private_token=config.GITLAB_TOKEN, ssl_verify=False)
        self.start_date = datetime.strptime(config.START_DATE, "%Y-%m-%d")
        self.end_date = datetime.strptime(config.END_DATE, "%Y-%m-%d")
        
        # 確保輸出目錄存在
        os.makedirs(config.OUTPUT_DIR, exist_ok=True)
        
    def get_all_projects(self) -> List[Any]:
        """取得所有專案"""
        print("正在取得專案列表...")
        projects = []
        
        if config.TARGET_GROUP_ID:
            group = self.gl.groups.get(config.TARGET_GROUP_ID)
            projects = group.projects.list(all=True)
        elif config.TARGET_PROJECT_IDS:
            projects = [self.gl.projects.get(pid) for pid in config.TARGET_PROJECT_IDS]
        else:
            projects = self.gl.projects.list(all=True)
        
        print(f"找到 {len(projects)} 個專案")
        return projects
    
    def get_commits_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集所有開發者的 commit 資料"""
        print("\n正在收集 Commit 資料...")
        commits_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                project_obj = self.gl.projects.get(project.id)
                commits = project_obj.commits.list(
                    all=True,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    commit_detail = project_obj.commits.get(commit.id)
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
        output_file = os.path.join(config.OUTPUT_DIR, "all-user.commits.csv")
        df.to_csv(output_file, index=False, encoding='utf-8-sig')
        print(f"✓ Commit 資料已儲存至: {output_file}")
        return df
    
    def get_code_changes_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集程式碼異動詳細資料"""
        print("\n正在收集程式碼異動資料...")
        changes_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                project_obj = self.gl.projects.get(project.id)
                commits = project_obj.commits.list(
                    all=True,
                    since=self.start_date.isoformat(),
                    until=self.end_date.isoformat()
                )
                
                for commit in commits:
                    try:
                        commit_detail = project_obj.commits.get(commit.id)
                        diffs = commit_detail.diff()
                        
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
        output_file = os.path.join(config.OUTPUT_DIR, "all-user.code-changes.csv")
        df.to_csv(output_file, index=False, encoding='utf-8-sig')
        print(f"✓ 程式碼異動資料已儲存至: {output_file}")
        return df
    
    def get_merge_requests_data(self, projects: List[Any]) -> pd.DataFrame:
        """收集 Code Review (Merge Request) 資料"""
        print("\n正在收集 Code Review 資料...")
        mr_data = []
        
        for project in tqdm(projects, desc="處理專案"):
            try:
                project_obj = self.gl.projects.get(project.id)
                mrs = project_obj.mergerequests.list(
                    all=True,
                    updated_after=self.start_date.isoformat(),
                    updated_before=self.end_date.isoformat()
                )
                
                for mr in mrs:
                    try:
                        mr_detail = project_obj.mergerequests.get(mr.iid)
                        
                        # 取得討論/評論
                        discussions = mr_detail.discussions.list(all=True)
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
        output_file = os.path.join(config.OUTPUT_DIR, "all-user.merge-requests.csv")
        df.to_csv(output_file, index=False, encoding='utf-8-sig')
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
        
        output_file = os.path.join(config.OUTPUT_DIR, "all-user.statistics.csv")
        stats_df.to_csv(output_file, index=False, encoding='utf-8-sig')
        print(f"✓ 統計資料已儲存至: {output_file}")
        return stats_df

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
