"""
匯出所有 GitLab 專案到 CSV 檔案

透過 GitLab API 取得所有專案資訊，並輸出為 CSV 格式
"""

import csv
import os
from gitlab_client import GitLabClient
from config import GITLAB_URL, GITLAB_TOKEN, OUTPUT_DIR


def export_all_projects():
    """匯出所有專案到 CSV 檔案"""
    
    # 初始化 GitLab 客戶端
    print(f"連線到 GitLab: {GITLAB_URL}")
    client = GitLabClient(GITLAB_URL, GITLAB_TOKEN)
    
    # 取得所有專案
    print("正在取得所有專案...")
    projects = client.get_projects()
    print(f"找到 {len(projects)} 個專案")
    
    # 準備輸出目錄
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    output_file = os.path.join(OUTPUT_DIR, "all-projects.csv")
    
    # 定義 CSV 欄位
    fieldnames = [
        'id',
        'name',
        'path',
        'path_with_namespace',
        'description',
        'visibility',
        'default_branch',
        'web_url',
        'ssh_url_to_repo',
        'http_url_to_repo',
        'namespace_id',
        'namespace_name',
        'namespace_path',
        'namespace_kind',
        'created_at',
        'last_activity_at',
        'archived',
        'star_count',
        'forks_count',
        'open_issues_count',
        'creator_id',
        'creator_name'
    ]
    
    # 寫入 CSV 檔案
    print(f"正在寫入 CSV 檔案: {output_file}")
    with open(output_file, 'w', newline='', encoding='utf-8-sig') as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()
        
        for idx, project in enumerate(projects, 1):
            # 取得完整專案資訊
            try:
                full_project = client.get_project(project.id)
                
                row = {
                    'id': full_project.id,
                    'name': full_project.name,
                    'path': full_project.path,
                    'path_with_namespace': full_project.path_with_namespace,
                    'description': getattr(full_project, 'description', '') or '',
                    'visibility': getattr(full_project, 'visibility', ''),
                    'default_branch': getattr(full_project, 'default_branch', ''),
                    'web_url': full_project.web_url,
                    'ssh_url_to_repo': getattr(full_project, 'ssh_url_to_repo', ''),
                    'http_url_to_repo': getattr(full_project, 'http_url_to_repo', ''),
                    'namespace_id': full_project.namespace.get('id', ''),
                    'namespace_name': full_project.namespace.get('name', ''),
                    'namespace_path': full_project.namespace.get('path', ''),
                    'namespace_kind': full_project.namespace.get('kind', ''),
                    'created_at': getattr(full_project, 'created_at', ''),
                    'last_activity_at': getattr(full_project, 'last_activity_at', ''),
                    'archived': getattr(full_project, 'archived', False),
                    'star_count': getattr(full_project, 'star_count', 0),
                    'forks_count': getattr(full_project, 'forks_count', 0),
                    'open_issues_count': getattr(full_project, 'open_issues_count', 0),
                    'creator_id': getattr(full_project, 'creator_id', ''),
                    'creator_name': ''
                }
                
                # 嘗試取得建立者名稱
                if hasattr(full_project, 'owner') and full_project.owner:
                    row['creator_name'] = full_project.owner.get('name', '')
                
                writer.writerow(row)
                print(f"  [{idx}/{len(projects)}] {full_project.path_with_namespace}")
                
            except Exception as e:
                print(f"  [錯誤] 無法取得專案 {project.id}: {e}")
                continue
    
    print(f"\n✅ 完成！匯出 {len(projects)} 個專案到 {output_file}")


if __name__ == "__main__":
    export_all_projects()
