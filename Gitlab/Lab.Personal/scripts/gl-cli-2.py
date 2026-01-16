#!/usr/bin/env python3
"""
GitLab CLI 2 - 開發者程式碼品質與技術水平分析工具

功能：
- 取得用戶詳細資訊（commits, code changes, code review, 統計資訊, 授權資訊）
- 支援多用戶、多專案查詢
- 輸出 Markdown 和 CSV 格式
"""

import argparse
import sys
import os
from pathlib import Path
from typing import Optional, List, Dict, Any
from datetime import datetime
import pandas as pd
import urllib3

# 抑制 SSL 警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

from gitlab_client import GitLabClient
import config


class UserDetailsAnalyzer:
    """用戶詳細資訊分析器"""
    
    def __init__(self, client: GitLabClient, output_dir: str = "./output-2"):
        self.client = client
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(exist_ok=True)
    
    def analyze_users(
        self, 
        usernames: Optional[List[str]] = None,
        project_names: Optional[List[str]] = None,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """
        分析用戶資訊（統一方法，支援單個、多個或所有用戶）
        
        Args:
            usernames: 用戶名稱列表，None 或空列表表示所有用戶
            project_names: 專案名稱列表，None 或空列表表示所有專案
            start_date: 開始日期 (YYYY-MM-DD)
            end_date: 結束日期 (YYYY-MM-DD)
            
        Returns:
            所有用戶分析結果的列表
        """
        print("🔍 開始分析用戶資訊...")
        
        # 1. 取得目標專案
        projects = self._get_target_projects(project_names)
        if not projects:
            print("⚠️  未找到任何專案")
            return []
        
        print(f"📊 找到 {len(projects)} 個專案")
        
        # 2. 取得目標用戶
        target_users = self._get_target_users(usernames, projects)
        if not target_users:
            print("⚠️  未找到任何用戶")
            return []
        
        print(f"👥 找到 {len(target_users)} 個用戶")
        
        # 3. 分析每個用戶
        results = []
        for idx, user_info in enumerate(target_users, 1):
            print(f"\n[{idx}/{len(target_users)}] 分析用戶: {user_info['username']}")
            user_data = self._analyze_single_user(
                user_info, 
                projects, 
                start_date, 
                end_date
            )
            results.append(user_data)
        
        print("\n✅ 所有分析完成!")
        return results
    
    def _get_target_projects(self, project_names: Optional[List[str]]) -> List[Any]:
        """取得目標專案列表"""
        if project_names and len(project_names) > 0:
            return self.client.get_projects(searches=project_names)
        else:
            if config.TARGET_GROUP_ID:
                return self.client.get_projects(group_id=config.TARGET_GROUP_ID)
            elif config.TARGET_PROJECT_IDS:
                return self.client.get_projects(project_ids=config.TARGET_PROJECT_IDS)
            else:
                return self.client.get_projects()
    
    def _get_target_users(
        self, 
        usernames: Optional[List[str]], 
        projects: List[Any]
    ) -> List[Dict[str, Any]]:
        """取得目標用戶列表"""
        if usernames and len(usernames) > 0:
            # 指定用戶名稱
            all_users = self.client.get_all_users()
            return [
                {'id': u.id, 'username': u.username, 'name': u.name, 'email': getattr(u, 'email', '')}
                for u in all_users 
                if u.username in usernames
            ]
        else:
            # 所有用戶：從專案貢獻者中收集
            user_dict = {}
            for project in projects:
                try:
                    contributors = project.repository_contributors()
                    for contrib in contributors:
                        email = contrib.get('email', '')
                        if email and email not in user_dict:
                            user_dict[email] = {
                                'username': contrib.get('name', email.split('@')[0]),
                                'name': contrib.get('name', ''),
                                'email': email,
                                'id': None
                            }
                except Exception as e:
                    print(f"  ⚠️  無法取得專案 {project.name} 的貢獻者: {e}")
            
            return list(user_dict.values())
    
    
    def _analyze_single_user(
        self,
        user_info: Dict[str, Any],
        projects: List[Any],
        start_date: Optional[str],
        end_date: Optional[str]
    ) -> Dict[str, Any]:
        """分析單一用戶並返回資料"""
        username = user_info['username']
        user_email = user_info['email']
        
        # 收集所有資料
        all_data = {
            'user_profile': [],
            'commits': [],
            'code_changes': [],
            'merge_requests': [],
            'code_reviews': [],
            'contributors': [],
            'permissions': [],
            'statistics': []
        }
        
        # 遍歷每個專案
        for project in projects:
            print(f"  📁 專案: {project.name}")
            
            try:
                # 1. 貢獻者統計
                contrib_data = self._get_contributor_stats(project, user_email)
                if contrib_data:
                    all_data['contributors'].extend(contrib_data)
                
                # 2. Commits
                commits_data = self._get_commits(project, user_email, start_date, end_date)
                all_data['commits'].extend(commits_data)
                
                # 3. Code Changes (從 commits 取得)
                changes_data = self._get_code_changes(project, commits_data)
                all_data['code_changes'].extend(changes_data)
                
                # 4. Merge Requests
                mr_data = self._get_merge_requests(project, username, start_date, end_date)
                all_data['merge_requests'].extend(mr_data)
                
                # 5. Code Reviews
                review_data = self._get_code_reviews(project, mr_data)
                all_data['code_reviews'].extend(review_data)
                
                # 6. Permissions
                perm_data = self._get_permissions(project, username)
                if perm_data:
                    all_data['permissions'].extend(perm_data)
                
            except Exception as e:
                print(f"    ⚠️  處理專案 {project.name} 時發生錯誤: {e}")
        
        # 7. User Profile (只取一次)
        if user_info['id']:
            all_data['user_profile'] = self._get_user_profile(user_info['id'])
        
        # 8. 統計資訊
        all_data['statistics'] = self._calculate_statistics(all_data)
        
        # 9. 輸出檔案
        self._save_results(username, all_data)
        
        return all_data
    
    def _get_contributor_stats(self, project: Any, user_email: str) -> List[Dict]:
        """取得貢獻者統計"""
        try:
            contributors = project.repository_contributors()
            result = []
            for contrib in contributors:
                if contrib.get('email') == user_email:
                    result.append({
                        'project_name': project.name,
                        'project_id': project.id,
                        'name': contrib.get('name', ''),
                        'email': contrib.get('email', ''),
                        'commits': contrib.get('commits', 0),
                        'additions': contrib.get('additions', 0),
                        'deletions': contrib.get('deletions', 0)
                    })
            return result
        except:
            return []
    
    def _get_commits(
        self, 
        project: Any, 
        user_email: str,
        start_date: Optional[str],
        end_date: Optional[str]
    ) -> List[Dict]:
        """取得 commits"""
        try:
            commits = self.client.get_project_commits(
                project.id,
                since=start_date,
                until=end_date
            )
            
            result = []
            for commit in commits:
                if commit.author_email == user_email:
                    commit_detail = self.client.get_commit_detail(project.id, commit.id)
                    result.append({
                        'project_name': project.name,
                        'project_id': project.id,
                        'commit_id': commit.id,
                        'short_id': commit.short_id,
                        'title': commit.title,
                        'message': commit.message,
                        'author_name': commit.author_name,
                        'author_email': commit.author_email,
                        'authored_date': commit.authored_date,
                        'committed_date': commit.committed_date,
                        'additions': getattr(commit.stats, 'additions', 0) if hasattr(commit, 'stats') else 0,
                        'deletions': getattr(commit.stats, 'deletions', 0) if hasattr(commit, 'stats') else 0,
                        'total': getattr(commit.stats, 'total', 0) if hasattr(commit, 'stats') else 0,
                        'web_url': commit.web_url
                    })
            return result
        except Exception as e:
            print(f"    ⚠️  無法取得 commits: {e}")
            return []
    
    def _get_code_changes(self, project: Any, commits_data: List[Dict]) -> List[Dict]:
        """取得程式碼變更詳情"""
        result = []
        for commit in commits_data[:50]:  # 限制數量避免過多 API 呼叫
            try:
                diffs = self.client.get_commit_diff(project.id, commit['commit_id'])
                for diff in diffs:
                    result.append({
                        'project_name': commit['project_name'],
                        'commit_id': commit['commit_id'],
                        'commit_title': commit['title'],
                        'file_path': diff.get('new_path', diff.get('old_path', '')),
                        'old_path': diff.get('old_path', ''),
                        'new_path': diff.get('new_path', ''),
                        'new_file': diff.get('new_file', False),
                        'renamed_file': diff.get('renamed_file', False),
                        'deleted_file': diff.get('deleted_file', False),
                        'diff': diff.get('diff', '')[:500]  # 限制長度
                    })
            except:
                continue
        return result
    
    def _get_merge_requests(
        self,
        project: Any,
        username: str,
        start_date: Optional[str],
        end_date: Optional[str]
    ) -> List[Dict]:
        """取得 Merge Requests"""
        try:
            mrs = self.client.get_project_merge_requests(
                project.id,
                updated_after=start_date,
                updated_before=end_date
            )
            
            result = []
            for mr in mrs:
                if mr.author.get('username') == username:
                    result.append({
                        'project_name': project.name,
                        'project_id': project.id,
                        'mr_iid': mr.iid,
                        'mr_id': mr.id,
                        'title': mr.title,
                        'description': mr.description or '',
                        'state': mr.state,
                        'created_at': mr.created_at,
                        'updated_at': mr.updated_at,
                        'merged_at': getattr(mr, 'merged_at', None),
                        'closed_at': getattr(mr, 'closed_at', None),
                        'author': mr.author.get('username'),
                        'source_branch': mr.source_branch,
                        'target_branch': mr.target_branch,
                        'user_notes_count': getattr(mr, 'user_notes_count', 0),
                        'upvotes': getattr(mr, 'upvotes', 0),
                        'downvotes': getattr(mr, 'downvotes', 0),
                        'web_url': mr.web_url
                    })
            return result
        except Exception as e:
            print(f"    ⚠️  無法取得 MRs: {e}")
            return []
    
    def _get_code_reviews(self, project: Any, mr_data: List[Dict]) -> List[Dict]:
        """取得 Code Review 資訊"""
        result = []
        for mr in mr_data[:20]:  # 限制數量
            try:
                discussions = self.client.get_merge_request_discussions(
                    project.id,
                    mr['mr_iid']
                )
                
                for discussion in discussions:
                    for note in discussion.attributes.get('notes', []):
                        result.append({
                            'project_name': mr['project_name'],
                            'mr_iid': mr['mr_iid'],
                            'mr_title': mr['title'],
                            'comment_author': note.get('author', {}).get('username', ''),
                            'comment_body': note.get('body', '')[:200],
                            'created_at': note.get('created_at', ''),
                            'resolved': note.get('resolved', False)
                        })
            except:
                continue
        return result
    
    def _get_permissions(self, project: Any, username: str) -> List[Dict]:
        """取得授權資訊"""
        try:
            members = project.members.list(all=True)
            result = []
            for member in members:
                if member.username == username:
                    from gitlab_client import GitLabClient
                    result.append({
                        'project_name': project.name,
                        'project_id': project.id,
                        'username': member.username,
                        'name': getattr(member, 'name', ''),
                        'access_level': member.access_level,
                        'access_level_name': self._get_access_level_name(member.access_level)
                    })
            return result
        except:
            return []
    
    def _get_access_level_name(self, level: int) -> str:
        """轉換授權等級"""
        levels = {
            10: 'Guest',
            20: 'Reporter',
            30: 'Developer',
            40: 'Maintainer',
            50: 'Owner'
        }
        return levels.get(level, 'Unknown')
    
    def _get_user_profile(self, user_id: int) -> List[Dict]:
        """取得用戶個人資料"""
        try:
            users = self.client.get_all_users()
            for user in users:
                if user.id == user_id:
                    return [{
                        'id': user.id,
                        'username': user.username,
                        'name': user.name,
                        'email': getattr(user, 'email', ''),
                        'state': user.state,
                        'created_at': user.created_at,
                        'bio': getattr(user, 'bio', ''),
                        'location': getattr(user, 'location', ''),
                        'organization': getattr(user, 'organization', ''),
                        'job_title': getattr(user, 'job_title', ''),
                        'last_activity_on': getattr(user, 'last_activity_on', ''),
                        'is_admin': getattr(user, 'is_admin', False)
                    }]
        except:
            pass
        return []
    
    def _calculate_statistics(self, all_data: Dict) -> List[Dict]:
        """計算統計資訊"""
        stats = {
            'total_projects': len(set(c['project_name'] for c in all_data['commits'])),
            'total_commits': len(all_data['commits']),
            'total_additions': sum(c['additions'] for c in all_data['commits']),
            'total_deletions': sum(c['deletions'] for c in all_data['commits']),
            'total_mrs': len(all_data['merge_requests']),
            'merged_mrs': len([mr for mr in all_data['merge_requests'] if mr['state'] == 'merged']),
            'total_code_reviews': len(all_data['code_reviews']),
            'files_changed': len(all_data['code_changes'])
        }
        
        return [stats]
    
    def _save_results(self, username: str, all_data: Dict) -> None:
        """儲存結果"""
        # 判斷是單一用戶還是全部用戶
        is_all_users = username.startswith('all-') or len(all_data['user_profile']) == 0
        
        for data_type, data_list in all_data.items():
            if not data_list:
                continue
            
            # 檔名規則
            if is_all_users:
                base_name = f"all-users-{data_type}"
            else:
                base_name = f"{username}-user-{data_type}"
            
            # 儲存 CSV
            df = pd.DataFrame(data_list)
            csv_path = self.output_dir / f"{base_name}.csv"
            df.to_csv(csv_path, index=False, encoding='utf-8-sig')
            print(f"  ✅ 已儲存: {csv_path}")



def main():
    """主程式"""
    parser = argparse.ArgumentParser(
        description='GitLab 開發者程式碼品質與技術水平分析工具',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
範例:
  # 取得單一用戶的詳細資訊
  %(prog)s user-details --username alice
  %(prog)s user-details --username G2023018 --project-name web-components-vue3
  
  # 多個用戶和專案
  %(prog)s user-details --username G2023018 G2023017 --project-name web-api mobile-app
  
  # 分析所有用戶
  %(prog)s user-details
  %(prog)s user-details --project-name web-api
        """
    )
    
    # 建立子命令
    subparsers = parser.add_subparsers(dest='command', help='可用的命令', required=True)
    
    # user-details 子命令
    user_details_parser = subparsers.add_parser(
        'user-details',
        help='取得單一用戶的詳細資訊',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
範例:
  %(prog)s --username alice
  %(prog)s --username G2023018 --project-name web-components-vue3
  %(prog)s --username G2023018 G2023017 --project-name web-api mobile-app
  %(prog)s --project-name web-api  # 分析所有用戶
  %(prog)s  # 分析所有用戶和專案
        """
    )
    
    user_details_parser.add_argument(
        '--username',
        nargs='*',
        help='用戶名稱（可指定多個，用空格分隔）。不指定則分析所有用戶'
    )
    
    user_details_parser.add_argument(
        '--project-name',
        nargs='*',
        dest='project_names',
        help='專案名稱（可指定多個，用空格分隔）。不指定則分析所有專案'
    )
    
    user_details_parser.add_argument(
        '--start-date',
        help='開始日期 (YYYY-MM-DD)，預設使用 config.py 的 START_DATE'
    )
    
    user_details_parser.add_argument(
        '--end-date',
        help='結束日期 (YYYY-MM-DD)，預設使用 config.py 的 END_DATE'
    )
    
    user_details_parser.add_argument(
        '--output',
        default='./output-2',
        help='輸出目錄，預設為 ./output-2'
    )
    
    args = parser.parse_args()
    
    # 初始化 GitLab 客戶端
    client = GitLabClient(
        gitlab_url=config.GITLAB_URL,
        private_token=config.GITLAB_TOKEN,
        ssl_verify=False
    )
    
    # 處理 user-details 子命令
    if args.command == 'user-details':
        start_date = args.start_date or config.START_DATE
        end_date = args.end_date or config.END_DATE
        
        print("=" * 60)
        print("GitLab 開發者程式碼品質與技術水平分析工具")
        print("=" * 60)
        print(f"GitLab URL: {config.GITLAB_URL}")
        
        if args.username and len(args.username) > 0:
            print(f"用戶: {', '.join(args.username)}")
        else:
            print(f"用戶: 所有用戶")
        
        print(f"分析期間: {start_date} ~ {end_date}")
        print(f"輸出目錄: {args.output}")
        print("=" * 60)
        
        analyzer = UserDetailsAnalyzer(client, args.output)
        
        try:
            analyzer.analyze_users(
                usernames=args.username if args.username else None,
                project_names=args.project_names if args.project_names else None,
                start_date=start_date,
                end_date=end_date
            )
        except KeyboardInterrupt:
            print("\n\n⚠️  使用者中斷執行")
            sys.exit(1)
        except Exception as e:
            print(f"\n❌ 執行錯誤: {e}")
            import traceback
            traceback.print_exc()
            sys.exit(1)




if __name__ == '__main__':
    main()
