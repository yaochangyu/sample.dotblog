"""
匯出所有 GitLab 使用者到 CSV 檔案

透過 GitLab API 取得所有使用者資訊，並輸出為 CSV 格式
"""

import csv
import os
from gitlab_client import GitLabClient
from config import GITLAB_URL, GITLAB_TOKEN, OUTPUT_DIR


def export_all_users():
    """匯出所有使用者到 CSV 檔案"""
    
    # 初始化 GitLab 客戶端
    print(f"連線到 GitLab: {GITLAB_URL}")
    client = GitLabClient(GITLAB_URL, GITLAB_TOKEN)
    
    # 取得所有使用者
    print("正在取得所有使用者...")
    users = client.get_all_users()
    print(f"找到 {len(users)} 個使用者")
    
    # 準備輸出目錄
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    output_file = os.path.join(OUTPUT_DIR, "all-users.csv")
    
    # 定義 CSV 欄位
    fieldnames = [
        'id',
        'username',
        'name',
        'email',
        'state',
        'locked',
        'is_admin',
        'is_auditor',
        'two_factor_enabled',
        'external',
        'private_profile',
        'avatar_url',
        'web_url',
        'created_at',
        'confirmed_at',
        'last_sign_in_at',
        'current_sign_in_at',
        'last_activity_on',
        'projects_limit',
        'can_create_group',
        'can_create_project',
        'bio',
        'location',
        'organization',
        'job_title',
        'linkedin',
        'twitter',
        'discord',
        'github',
        'website_url',
        'namespace_id',
        'current_sign_in_ip',
        'last_sign_in_ip',
        'identities_count',
        'identity_providers'
    ]
    
    # 寫入 CSV 檔案
    print(f"正在寫入 CSV 檔案: {output_file}")
    with open(output_file, 'w', newline='', encoding='utf-8-sig') as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()
        
        for idx, user in enumerate(users, 1):
            try:
                # 處理身份提供者資訊
                identities = getattr(user, 'identities', [])
                identity_providers = ','.join([identity.get('provider', '') for identity in identities]) if identities else ''
                
                row = {
                    'id': user.id,
                    'username': user.username,
                    'name': user.name,
                    'email': getattr(user, 'email', ''),
                    'state': getattr(user, 'state', ''),
                    'locked': getattr(user, 'locked', False),
                    'is_admin': getattr(user, 'is_admin', False),
                    'is_auditor': getattr(user, 'is_auditor', False),
                    'two_factor_enabled': getattr(user, 'two_factor_enabled', False),
                    'external': getattr(user, 'external', False),
                    'private_profile': getattr(user, 'private_profile', False),
                    'avatar_url': getattr(user, 'avatar_url', ''),
                    'web_url': getattr(user, 'web_url', ''),
                    'created_at': getattr(user, 'created_at', ''),
                    'confirmed_at': getattr(user, 'confirmed_at', ''),
                    'last_sign_in_at': getattr(user, 'last_sign_in_at', ''),
                    'current_sign_in_at': getattr(user, 'current_sign_in_at', ''),
                    'last_activity_on': getattr(user, 'last_activity_on', ''),
                    'projects_limit': getattr(user, 'projects_limit', 0),
                    'can_create_group': getattr(user, 'can_create_group', False),
                    'can_create_project': getattr(user, 'can_create_project', False),
                    'bio': getattr(user, 'bio', ''),
                    'location': getattr(user, 'location', ''),
                    'organization': getattr(user, 'organization', ''),
                    'job_title': getattr(user, 'job_title', ''),
                    'linkedin': getattr(user, 'linkedin', ''),
                    'twitter': getattr(user, 'twitter', ''),
                    'discord': getattr(user, 'discord', ''),
                    'github': getattr(user, 'github', ''),
                    'website_url': getattr(user, 'website_url', ''),
                    'namespace_id': getattr(user, 'namespace_id', ''),
                    'current_sign_in_ip': getattr(user, 'current_sign_in_ip', ''),
                    'last_sign_in_ip': getattr(user, 'last_sign_in_ip', ''),
                    'identities_count': len(identities),
                    'identity_providers': identity_providers
                }
                
                writer.writerow(row)
                print(f"  [{idx}/{len(users)}] {user.username} ({user.name})")
                
            except Exception as e:
                print(f"  [錯誤] 無法處理使用者 {user.id}: {e}")
                continue
    
    print(f"\n✅ 完成！匯出 {len(users)} 個使用者到 {output_file}")


if __name__ == "__main__":
    export_all_users()
