#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GitLab 開發者資訊取得工具 (Python 版)
使用 GitLab API 取得所有開發者和專案資訊
"""

import requests
import sys
import json
from urllib3.exceptions import InsecureRequestWarning

# 關閉 SSL 警告（內部 GitLab 可能使用自簽憑證）
requests.packages.urllib3.disable_warnings(category=InsecureRequestWarning)

def get_gitlab_users(gitlab_url, token):
    """取得所有 GitLab 使用者"""
    headers = {'PRIVATE-TOKEN': token}
    url = f"{gitlab_url}/api/v4/users"
    
    try:
        response = requests.get(url, headers=headers, verify=False, params={'per_page': 100})
        response.raise_for_status()
        return response.json()
    except Exception as e:
        print(f"❌ 取得使用者失敗: {e}")
        return None

def get_gitlab_projects(gitlab_url, token):
    """取得所有可存取的專案"""
    headers = {'PRIVATE-TOKEN': token}
    url = f"{gitlab_url}/api/v4/projects"
    
    try:
        response = requests.get(url, headers=headers, verify=False, 
                              params={'per_page': 100, 'membership': 'true'})
        response.raise_for_status()
        return response.json()
    except Exception as e:
        print(f"❌ 取得專案失敗: {e}")
        return None

def get_project_members(gitlab_url, token, project_id):
    """取得專案成員"""
    headers = {'PRIVATE-TOKEN': token}
    url = f"{gitlab_url}/api/v4/projects/{project_id}/members/all"
    
    try:
        response = requests.get(url, headers=headers, verify=False)
        response.raise_for_status()
        return response.json()
    except Exception as e:
        return []

def test_connection(gitlab_url, token):
    """測試 GitLab API 連線"""
    headers = {'PRIVATE-TOKEN': token}
    url = f"{gitlab_url}/api/v4/user"
    
    try:
        response = requests.get(url, headers=headers, verify=False, timeout=10)
        if response.status_code == 200:
            user = response.json()
            return True, user.get('username', 'Unknown')
        else:
            return False, f"HTTP {response.status_code}"
    except Exception as e:
        return False, str(e)

def main():
    if len(sys.argv) < 3:
        print("""
╔══════════════════════════════════════════════════════════════╗
║         GitLab 開發者資訊取得工具 (Python 版)                ║
╚══════════════════════════════════════════════════════════════╝

📖 使用方式:
   python3 get_gitlab_developers.py <GitLab URL> <Access Token>

範例:
   python3 get_gitlab_developers.py https://192.168.1.158 glpat-xxxxxxxxxxxxx

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 如何取得 GitLab Access Token:

1. 登入 GitLab: https://192.168.1.158
2. 點選右上角頭像 > Preferences (設定)
3. 左側選單選擇 Access Tokens
4. 建立新的 Personal Access Token
   • Token name: developer-analyzer
   • Expiration: 設定過期時間
   • Scopes: 勾選以下權限
     ✓ read_api
     ✓ read_user
     ✓ read_repository
5. 點選 Create personal access token
6. 複製顯示的 token (只會顯示一次)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
""")
        sys.exit(1)
    
    gitlab_url = sys.argv[1].rstrip('/')
    token = sys.argv[2]
    
    print("╔══════════════════════════════════════════════════════════════╗")
    print("║         GitLab 開發者資訊取得工具                            ║")
    print("╚══════════════════════════════════════════════════════════════╝")
    print()
    print(f"🔍 正在連接 GitLab: {gitlab_url}")
    print()
    
    # 測試連線
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    print("1️⃣  測試 GitLab API 連線...")
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    
    success, result = test_connection(gitlab_url, token)
    
    if not success:
        print(f"❌ 連線失敗: {result}")
        print()
        print("可能原因:")
        print("  • Token 無效或過期")
        print(f"  • 網路無法連到 {gitlab_url}")
        print("  • Token 權限不足")
        print()
        sys.exit(1)
    
    print(f"✅ 連線成功！當前使用者: {result}")
    print()
    
    # 取得所有使用者
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    print("2️⃣  取得所有 GitLab 使用者...")
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    
    users = get_gitlab_users(gitlab_url, token)
    
    if users:
        print(f"\n找到 {len(users)} 位使用者:\n")
        print(f"{'ID':<8} {'使用者名稱':<20} {'姓名':<25} {'Email':<35}")
        print("─" * 90)
        
        for user in users:
            user_id = str(user.get('id', 'N/A'))
            username = user.get('username', 'N/A')
            name = user.get('name', 'N/A')
            email = user.get('email') or user.get('public_email') or 'N/A'
            
            print(f"{user_id:<8} {username:<20} {name:<25} {email:<35}")
        
        # 儲存到檔案
        output_file = './output/gitlab_users.json'
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(users, f, ensure_ascii=False, indent=2)
        print(f"\n💾 已儲存到: {output_file}")
    
    print()
    
    # 取得所有專案
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    print("3️⃣  取得所有可存取的專案...")
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    
    projects = get_gitlab_projects(gitlab_url, token)
    
    if projects:
        print(f"\n找到 {len(projects)} 個專案:\n")
        print(f"{'ID':<8} {'專案名稱':<50} {'HTTP URL':<60}")
        print("─" * 120)
        
        project_list = []
        
        for proj in projects:
            proj_id = proj.get('id')
            name = proj.get('name_with_namespace', proj.get('name', 'N/A'))
            http_url = proj.get('http_url_to_repo', 'N/A')
            
            print(f"{proj_id:<8} {name:<50} {http_url:<60}")
            
            project_list.append({
                'id': proj_id,
                'name': name,
                'http_url': http_url,
                'ssh_url': proj.get('ssh_url_to_repo'),
                'web_url': proj.get('web_url')
            })
        
        # 儲存到檔案
        output_file = './output/gitlab_projects.json'
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(project_list, f, ensure_ascii=False, indent=2)
        print(f"\n💾 已儲存到: {output_file}")
    
    print()
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    print("✅ 資料取得完成")
    print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    print()
    print("💡 下一步:")
    print("   1. 選擇要分析的專案")
    print("   2. clone 專案到本地:")
    print("      git clone <專案 URL>")
    print("   3. 使用 developer_analyzer.py 分析開發者")
    print()
    print("📁 輸出檔案:")
    print("   • output/gitlab_users.json    - 所有使用者資訊")
    print("   • output/gitlab_projects.json - 所有專案清單")
    print()

if __name__ == "__main__":
    main()
