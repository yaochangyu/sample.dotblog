"""
GitLab CLI - 統一的命令列介面工具
支援查詢使用者資訊、專案資訊
"""

import argparse
import sys
from typing import Optional, List
from gitlab_collector import GitLabCollector
from gitlab_developer_collector import GitLabDeveloperCollector
import config


def parse_project_ids(project_ids_str: Optional[str]) -> Optional[List[int]]:
    """解析專案 ID 字串為列表"""
    if not project_ids_str:
        return None
    try:
        return [int(pid.strip()) for pid in project_ids_str.split(',')]
    except ValueError:
        print("錯誤: 專案 ID 必須是數字，多個 ID 用逗號分隔")
        sys.exit(1)


def cmd_user_info(args):
    """執行使用者資訊查詢"""
    print("=" * 70)
    print("GitLab 使用者資訊查詢")
    print("=" * 70)
    
    project_ids = parse_project_ids(args.project_id)
    
    # 判斷是查詢特定開發者還是所有開發者
    if args.developer_email or args.developer_username:
        # 查詢特定開發者
        print(f"\n查詢開發者: {args.developer_email or args.developer_username}")
        
        collector = GitLabDeveloperCollector(
            developer_email=args.developer_email,
            developer_username=args.developer_username,
            start_date=args.start_date,
            end_date=args.end_date,
            project_ids=project_ids,
            group_id=args.group_id
        )
        
        # 1. 取得專案
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
        identifier = args.developer_email or args.developer_username
        safe_identifier = identifier.replace('@', '_at_').replace('.', '_')
        report_file = collector.save_dataframe(
            stats_df,  # 使用 DataFrame 作為佔位，實際上我們會直接寫文字檔
            f"{safe_identifier}.report.txt"
        ).replace('.txt.csv', '.txt')  # 修正副檔名
        
        # 重新寫入正確的報告格式
        import os
        report_file_correct = os.path.join(config.OUTPUT_DIR, f"{safe_identifier}.report.txt")
        with open(report_file_correct, 'w', encoding='utf-8') as f:
            f.write(report)
        print(f"✓ 報告已儲存至: {report_file_correct}")
        
    else:
        # 查詢所有開發者
        print("\n查詢所有開發者資訊")
        
        collector = GitLabCollector(
            start_date=args.start_date,
            end_date=args.end_date,
            project_ids=project_ids,
            group_id=args.group_id
        )
        
        # 1. 取得專案
        projects = collector.get_all_projects()
        
        # 2. 收集 Commit 資料
        commits_df = collector.get_commits_data(projects)
        
        # 3. 收集 Code Review 資料
        mr_df = collector.get_merge_requests_data(projects)
        
        # 4. 收集專案資料
        projects_df = collector.get_projects_data(projects)
        
        # 5. 產生統計資料
        stats_df = collector.get_statistics_data(commits_df, mr_df, projects_df)
        
        print(f"\n✓ 完成！共收集 {len(commits_df)} 筆 commits")
    
    print("\n" + "=" * 70)
    print("✓ 查詢完成！")
    print("=" * 70)


def cmd_project_info(args):
    """執行專案資訊查詢"""
    print("=" * 70)
    print("GitLab 專案資訊查詢")
    print("=" * 70)
    
    project_ids = parse_project_ids(args.project_id)
    
    collector = GitLabCollector(
        project_ids=project_ids,
        group_id=args.group_id
    )
    
    # 1. 取得專案
    projects = collector.get_all_projects()
    
    # 2. 收集專案資料
    projects_df = collector.get_projects_data(projects)
    
    print(f"\n✓ 完成！共收集 {len(projects_df)} 個專案資訊")
    
    print("\n" + "=" * 70)
    print("✓ 查詢完成！")
    print("=" * 70)


def main():
    """主程式"""
    parser = argparse.ArgumentParser(
        description='GitLab CLI - 統一的命令列介面工具',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
使用範例:

  # 查詢所有使用者資訊 (指定時間範圍)
  python gitlab_cli.py user-info --start-date 2024-01-01 --end-date 2024-12-31

  # 查詢特定使用者資訊 (by email)
  python gitlab_cli.py user-info --developer-email user@example.com

  # 查詢特定使用者資訊 (by username)
  python gitlab_cli.py user-info --developer-username johndoe

  # 查詢特定專案的使用者資訊
  python gitlab_cli.py user-info --project-id 123,456

  # 查詢所有專案資訊
  python gitlab_cli.py project-info

  # 查詢特定專案資訊
  python gitlab_cli.py project-info --project-id 123,456
        """
    )
    
    subparsers = parser.add_subparsers(dest='command', help='可用的命令')
    subparsers.required = True
    
    # user-info 命令
    user_info_parser = subparsers.add_parser(
        'user-info',
        help='查詢使用者資訊 (commits, code-changes, merge-requests, statistics)'
    )
    user_info_parser.add_argument(
        '--start-date',
        type=str,
        help=f'開始時間 (格式: YYYY-MM-DD)，預設: {config.START_DATE}'
    )
    user_info_parser.add_argument(
        '--end-date',
        type=str,
        help=f'結束時間 (格式: YYYY-MM-DD)，預設: {config.END_DATE}'
    )
    user_info_parser.add_argument(
        '--developer-email',
        type=str,
        help='特定開發者 email (例如: user@example.com)'
    )
    user_info_parser.add_argument(
        '--developer-username',
        type=str,
        help='特定開發者 username (例如: johndoe)'
    )
    user_info_parser.add_argument(
        '--project-id',
        type=str,
        help='特定專案 ID (多個用逗號分隔，例如: 123,456)'
    )
    user_info_parser.add_argument(
        '--group-id',
        type=int,
        help=f'指定群組 ID，預設: {config.TARGET_GROUP_ID}'
    )
    user_info_parser.set_defaults(func=cmd_user_info)
    
    # project-info 命令
    project_info_parser = subparsers.add_parser(
        'project-info',
        help='查詢專案資訊'
    )
    project_info_parser.add_argument(
        '--project-id',
        type=str,
        help='特定專案 ID (多個用逗號分隔，例如: 123,456)'
    )
    project_info_parser.add_argument(
        '--group-id',
        type=int,
        help=f'指定群組 ID，預設: {config.TARGET_GROUP_ID}'
    )
    project_info_parser.set_defaults(func=cmd_project_info)
    
    # 解析參數並執行對應的命令
    args = parser.parse_args()
    
    try:
        args.func(args)
    except KeyboardInterrupt:
        print("\n\n操作已取消")
        sys.exit(0)
    except Exception as e:
        print(f"\n錯誤: {str(e)}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
