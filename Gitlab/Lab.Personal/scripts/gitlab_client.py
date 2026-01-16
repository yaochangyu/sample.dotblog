"""
GitLab API 操作封裝類別

統一管理所有與 GitLab API 的直接互動，提供：
- 專案操作
- Commits 操作
- Merge Requests 操作
- 使用者操作
- 群組操作
"""

import gitlab
from typing import List, Dict, Any, Optional
from datetime import datetime


class GitLabClient:
    """GitLab API 操作封裝類別"""
    
    def __init__(self, gitlab_url: str, private_token: str, ssl_verify: bool = False):
        """
        初始化 GitLab 客戶端
        
        Args:
            gitlab_url: GitLab 伺服器 URL
            private_token: 私人存取權杖
            ssl_verify: 是否驗證 SSL 憑證
        """
        self.gl = gitlab.Gitlab(gitlab_url, private_token=private_token, ssl_verify=ssl_verify)
    
    # ==================== 專案操作 ====================
    
    def get_projects(
        self, 
        group_id: Optional[int] = None, 
        project_ids: Optional[List[int]] = None
    ) -> List[Any]:
        """
        取得專案列表
        
        Args:
            group_id: 群組 ID (若指定則取得該群組的專案)
            project_ids: 專案 ID 列表 (若指定則取得這些專案)
        
        Returns:
            專案物件列表
        """
        if group_id:
            group = self.gl.groups.get(group_id)
            return group.projects.list(all=True)
        elif project_ids:
            return [self.gl.projects.get(pid) for pid in project_ids]
        else:
            return self.gl.projects.list(all=True)
    
    def get_project(self, project_id: int) -> Any:
        """
        取得單一專案
        
        Args:
            project_id: 專案 ID
        
        Returns:
            專案物件
        """
        return self.gl.projects.get(project_id)
    
    # ==================== Commits 操作 ====================
    
    def get_project_commits(
        self, 
        project_id: int, 
        since: Optional[str] = None,
        until: Optional[str] = None
    ) -> List[Any]:
        """
        取得專案的 commits
        
        Args:
            project_id: 專案 ID
            since: 起始日期 (ISO 格式)
            until: 結束日期 (ISO 格式)
        
        Returns:
            commit 物件列表
        """
        project = self.gl.projects.get(project_id)
        params = {'all': True}
        if since:
            params['since'] = since
        if until:
            params['until'] = until
        
        return project.commits.list(**params)
    
    def get_commit_detail(self, project_id: int, commit_id: str) -> Any:
        """
        取得 commit 詳細資訊
        
        Args:
            project_id: 專案 ID
            commit_id: commit ID
        
        Returns:
            commit 詳細資訊物件
        """
        project = self.gl.projects.get(project_id)
        return project.commits.get(commit_id)
    
    def get_commit_diff(self, project_id: int, commit_id: str) -> List[Dict[str, Any]]:
        """
        取得 commit 的 diff
        
        Args:
            project_id: 專案 ID
            commit_id: commit ID
        
        Returns:
            diff 列表
        """
        commit_detail = self.get_commit_detail(project_id, commit_id)
        return commit_detail.diff(get_all=True)
    
    # ==================== Merge Requests 操作 ====================
    
    def get_project_merge_requests(
        self,
        project_id: int,
        updated_after: Optional[str] = None,
        updated_before: Optional[str] = None
    ) -> List[Any]:
        """
        取得專案的 Merge Requests
        
        Args:
            project_id: 專案 ID
            updated_after: 更新時間起始 (ISO 格式)
            updated_before: 更新時間結束 (ISO 格式)
        
        Returns:
            MR 物件列表
        """
        project = self.gl.projects.get(project_id)
        params = {'all': True}
        if updated_after:
            params['updated_after'] = updated_after
        if updated_before:
            params['updated_before'] = updated_before
        
        return project.mergerequests.list(**params)
    
    def get_merge_request_detail(self, project_id: int, mr_iid: int) -> Any:
        """
        取得 Merge Request 詳細資訊
        
        Args:
            project_id: 專案 ID
            mr_iid: MR 內部 ID
        
        Returns:
            MR 詳細資訊物件
        """
        project = self.gl.projects.get(project_id)
        return project.mergerequests.get(mr_iid)
    
    def get_merge_request_discussions(self, project_id: int, mr_iid: int) -> List[Any]:
        """
        取得 Merge Request 的討論
        
        Args:
            project_id: 專案 ID
            mr_iid: MR 內部 ID
        
        Returns:
            討論物件列表
        """
        mr_detail = self.get_merge_request_detail(project_id, mr_iid)
        return mr_detail.discussions.list(all=True)
    
    def get_merge_request_changes(self, project_id: int, mr_iid: int) -> Dict[str, Any]:
        """
        取得 Merge Request 的變更
        
        Args:
            project_id: 專案 ID
            mr_iid: MR 內部 ID
        
        Returns:
            變更資訊字典
        """
        mr_detail = self.get_merge_request_detail(project_id, mr_iid)
        return mr_detail.changes()
    
    # ==================== 使用者操作 ====================
    
    def get_all_users(self) -> List[Any]:
        """
        取得所有使用者
        
        Returns:
            使用者物件列表
        """
        return self.gl.users.list(all=True)
    
    # ==================== 群組操作 ====================
    
    def get_group(self, group_id: int) -> Any:
        """
        取得群組
        
        Args:
            group_id: 群組 ID
        
        Returns:
            群組物件
        """
        return self.gl.groups.get(group_id)
    
    def get_groups(self, group_name: Optional[str] = None) -> List[Any]:
        """
        取得群組列表
        
        Args:
            group_name: 群組名稱 (可選，若指定則搜尋符合的群組)
        
        Returns:
            群組物件列表
        """
        if group_name:
            # 搜尋特定群組
            return self.gl.groups.list(search=group_name, all=True)
        else:
            # 取得所有群組
            return self.gl.groups.list(all=True)
    
    def get_group_subgroups(self, group_id: int) -> List[Any]:
        """
        取得群組的子群組
        
        Args:
            group_id: 群組 ID
        
        Returns:
            子群組列表
        """
        group = self.gl.groups.get(group_id)
        return group.subgroups.list(all=True)
    
    def get_group_projects(self, group_id: int) -> List[Any]:
        """
        取得群組的專案
        
        Args:
            group_id: 群組 ID
        
        Returns:
            專案列表
        """
        group = self.gl.groups.get(group_id)
        return group.projects.list(all=True)
    
    def get_group_members(self, group_id: int) -> List[Any]:
        """
        取得群組成員
        
        Args:
            group_id: 群組 ID
        
        Returns:
            成員列表
        """
        group = self.gl.groups.get(group_id)
        return group.members.list(all=True)
