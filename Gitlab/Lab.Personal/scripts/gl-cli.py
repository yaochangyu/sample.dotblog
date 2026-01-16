#!/usr/bin/env python3
"""
GitLab CLI - 開發者程式碼品質與技術水平分析工具

遵循 SOLID 原則設計：
- S: 單一職責 - 每個類別只負責一個功能
- O: 開放封閉 - 透過介面擴展，不修改現有程式碼
- L: 里氏替換 - 子類別可以替換父類別
- I: 介面隔離 - 細分介面，避免實作不需要的方法
- D: 依賴反轉 - 依賴抽象而非具體實作
"""

import argparse
import sys
import os
from abc import ABC, abstractmethod
from typing import Optional, List, Dict, Any
from pathlib import Path
import pandas as pd
from datetime import datetime
import urllib3

# 抑制 SSL 不安全連線警告（self-signed certificates）
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

from gitlab_client import GitLabClient
import config


# ==================== 工具類別 ====================

class AccessLevelUtil:
    """GitLab 授權等級工具類別"""
    
    # 授權等級對照表
    LEVELS = {
        10: 'Guest',
        20: 'Reporter',
        30: 'Developer',
        40: 'Maintainer',
        50: 'Owner'
    }
    
    @staticmethod
    def get_level_name(level: int) -> str:
        """
        轉換存取等級為名稱
        
        Args:
            level: 存取等級代碼 (10/20/30/40/50)
        
        Returns:
            存取等級名稱
        """
        return AccessLevelUtil.LEVELS.get(level, 'Unknown')


# ==================== 抽象介面 (介面隔離原則) ====================

class IProgressReporter(ABC):
    """進度報告介面"""
    
    @abstractmethod
    def report_start(self, message: str) -> None:
        """報告開始訊息"""
        pass
    
    @abstractmethod
    def report_progress(self, current: int, total: int, message: str = "") -> None:
        """報告進度"""
        pass
    
    @abstractmethod
    def report_complete(self, message: str) -> None:
        """報告完成訊息"""
        pass
    
    @abstractmethod
    def report_warning(self, message: str) -> None:
        """報告警告訊息"""
        pass


class IDataFetcher(ABC):
    """資料獲取介面"""
    
    @abstractmethod
    def fetch(self, **kwargs) -> Any:
        """獲取資料"""
        pass


class IDataProcessor(ABC):
    """資料處理介面"""
    
    @abstractmethod
    def process(self, data: Any) -> pd.DataFrame:
        """處理資料"""
        pass


class IDataExporter(ABC):
    """資料匯出介面"""
    
    @abstractmethod
    def export(self, df: pd.DataFrame, filename: str) -> None:
        """匯出資料"""
        pass


# ==================== 進度報告類別 (單一職責原則) ====================

class ConsoleProgressReporter(IProgressReporter):
    """終端機進度報告器"""
    
    def report_start(self, message: str) -> None:
        """報告開始訊息"""
        print(f"\n🔄 {message}")
    
    def report_progress(self, current: int, total: int, message: str = "") -> None:
        """報告進度"""
        percentage = (current / total * 100) if total > 0 else 0
        bar_length = 30
        filled_length = int(bar_length * current // total) if total > 0 else 0
        bar = '█' * filled_length + '░' * (bar_length - filled_length)
        
        progress_msg = f"  [{bar}] {current}/{total} ({percentage:.1f}%)"
        if message:
            progress_msg += f" - {message}"
        
        print(f"\r{progress_msg}", end='', flush=True)
        
        if current >= total:
            print()  # 完成時換行
    
    def report_complete(self, message: str) -> None:
        """報告完成訊息"""
        print(f"✓ {message}")
    
    def report_warning(self, message: str) -> None:
        """報告警告訊息"""
        print(f"⚠️  {message}")


class SilentProgressReporter(IProgressReporter):
    """靜默進度報告器（不輸出任何訊息）"""
    
    def report_start(self, message: str) -> None:
        pass
    
    def report_progress(self, current: int, total: int, message: str = "") -> None:
        pass
    
    def report_complete(self, message: str) -> None:
        pass
    
    def report_warning(self, message: str) -> None:
        pass


# ==================== 資料獲取器 (單一職責原則) ====================

class ProjectDataFetcher(IDataFetcher):
    """專案資料獲取器（包含授權資訊）"""
    
    def __init__(self, client: GitLabClient, progress_reporter: Optional[IProgressReporter] = None):
        self.client = client
        self.progress = progress_reporter or SilentProgressReporter()
    
    def fetch(self, project_name: Optional[str] = None, 
              group_id: Optional[int] = None,
              include_permissions: bool = True) -> Dict[str, Any]:
        """
        獲取專案資料（包含授權資訊）
        
        Args:
            project_name: 專案名稱 (可選)
            group_id: 群組 ID (可選)
            include_permissions: 是否包含授權資訊 (預設: True)
        
        Returns:
            包含專案列表和授權資訊的字典
        """
        self.progress.report_start("正在獲取專案列表...")
        projects = self.client.get_projects(group_id=group_id)
        self.progress.report_complete(f"找到 {len(projects)} 個專案")
        
        if project_name:
            projects = [p for p in projects if project_name.lower() in p.name.lower()]
            self.progress.report_complete(f"篩選後剩餘 {len(projects)} 個專案")
        
        result = {
            'projects': projects,
            'permissions': []
        }
        
        # 如果需要包含授權資訊
        if include_permissions and projects:
            self.progress.report_start("正在獲取授權資訊...")
            for idx, project in enumerate(projects, 1):
                try:
                    self.progress.report_progress(idx, len(projects), project.name)
                    project_detail = self.client.get_project(project.id)
                    
                    # 獲取專案成員
                    members = project_detail.members.list(all=True)
                    
                    for member in members:
                        result['permissions'].append({
                            'project_id': project.id,
                            'project_name': project.name,
                            'member_type': 'User',
                            'member_id': member.id,
                            'member_name': member.name,
                            'member_username': member.username,
                            'member_email': getattr(member, 'email', ''),
                            'access_level': member.access_level,
                            'access_level_name': AccessLevelUtil.get_level_name(member.access_level)
                        })
                    
                    # 獲取群組成員（如果有共享給群組）
                    try:
                        shared_groups = project_detail.shared_with_groups
                        for group in shared_groups:
                            result['permissions'].append({
                                'project_id': project.id,
                                'project_name': project.name,
                                'member_type': 'Group',
                                'member_id': group['group_id'],
                                'member_name': group['group_name'],
                                'member_username': '',
                                'member_email': '',
                                'access_level': group['group_access_level'],
                                'access_level_name': AccessLevelUtil.get_level_name(group['group_access_level'])
                            })
                    except:
                        pass
                        
                except Exception as e:
                    self.progress.report_warning(f"無法獲取 {project.name} 的授權資訊: {e}")
                    continue
        
        return result


class ProjectPermissionFetcher(IDataFetcher):
    """專案授權資料獲取器"""
    
    def __init__(self, client: GitLabClient, progress_reporter: Optional[IProgressReporter] = None):
        self.client = client
        self.progress = progress_reporter or SilentProgressReporter()
    
    def fetch(self, project_name: Optional[str] = None,
              group_id: Optional[int] = None) -> List[Dict[str, Any]]:
        """
        獲取專案授權資料
        
        Args:
            project_name: 專案名稱 (可選)
            group_id: 群組 ID (可選)
        
        Returns:
            授權資料列表
        """
        self.progress.report_start("正在獲取專案列表...")
        projects = self.client.get_projects(group_id=group_id)
        self.progress.report_complete(f"找到 {len(projects)} 個專案")
        
        if project_name:
            projects = [p for p in projects if project_name.lower() in p.name.lower()]
            self.progress.report_complete(f"篩選後剩餘 {len(projects)} 個專案")
        
        permissions_data = []
        
        if projects:
            self.progress.report_start("正在獲取專案授權資訊...")
            for idx, project in enumerate(projects, 1):
                self.progress.report_progress(idx, len(projects), project.name)
                project_detail = self.client.get_project(project.id)
                
                # 獲取專案成員
                members = project_detail.members.list(all=True)
                
                for member in members:
                    permissions_data.append({
                        'project_id': project.id,
                        'project_name': project.name,
                        'member_type': 'User',
                        'member_id': member.id,
                        'member_name': member.name,
                        'member_username': member.username,
                        'member_email': getattr(member, 'email', ''),
                        'access_level': member.access_level,
                        'access_level_name': AccessLevelUtil.get_level_name(member.access_level)
                    })
                
                # 獲取群組成員（如果有共享給群組）
                try:
                    shared_groups = project_detail.shared_with_groups
                    for group in shared_groups:
                        permissions_data.append({
                            'project_id': project.id,
                            'project_name': project.name,
                            'member_type': 'Group',
                            'member_id': group['group_id'],
                            'member_name': group['group_name'],
                            'member_username': '',
                            'member_email': '',
                            'access_level': group['group_access_level'],
                            'access_level_name': AccessLevelUtil.get_level_name(group['group_access_level'])
                        })
                except:
                    pass
        
        return permissions_data


class UserDataFetcher(IDataFetcher):
    """使用者資料獲取器"""
    
    def __init__(self, client: GitLabClient, progress_reporter: Optional[IProgressReporter] = None):
        self.client = client
        self.progress = progress_reporter or SilentProgressReporter()
    
    def fetch(self, username: Optional[str] = None,
              project_name: Optional[str] = None,
              start_date: Optional[str] = None,
              end_date: Optional[str] = None,
              group_id: Optional[int] = None,
              user_info: Optional[Any] = None) -> Dict[str, Any]:
        """
        獲取使用者資料
        
        Args:
            username: 使用者名稱 (可選)
            project_name: 專案名稱 (可選，篩選特定專案)
            start_date: 開始日期
            end_date: 結束日期
            group_id: 群組 ID (可選)
            user_info: 使用者資訊物件 (可選，用於精確匹配)
        
        Returns:
            使用者資料字典
        """
        self.progress.report_start("正在獲取專案列表...")
        projects = self.client.get_projects(group_id=group_id)
        self.progress.report_complete(f"找到 {len(projects)} 個專案")
        
        # 如果指定了專案名稱，篩選專案
        if project_name:
            projects = [p for p in projects if project_name.lower() in p.name.lower()]
            if not projects:
                self.progress.report_warning(f"找不到名稱包含 '{project_name}' 的專案")
            else:
                self.progress.report_complete(f"篩選後剩餘 {len(projects)} 個專案")
        
        user_data = {
            'commits': [],
            'code_changes': [],
            'merge_requests': [],
            'code_reviews': [],
            'permissions': []
        }
        
        # 準備匹配條件（使用 email 和 name 進行精確匹配）
        target_email = None
        target_name = None
        target_username = username
        
        if user_info:
            target_email = getattr(user_info, 'email', None)
            target_name = getattr(user_info, 'name', None)
            target_username = getattr(user_info, 'username', username)
        
        if projects:
            self.progress.report_start(f"正在分析 {len(projects)} 個專案的使用者活動...")
        
        for idx, project in enumerate(projects, 1):
            self.progress.report_progress(idx, len(projects), project.name)
            
            # 獲取 commits
            commits = self.client.get_project_commits(
                project.id,
                since=start_date,
                until=end_date
            )
            
            for commit in commits:
                # 改善匹配邏輯：使用 email 優先，其次 name，最後 username
                if username:
                    match = False
                    if target_email and commit.author_email == target_email:
                        match = True
                    elif target_name and commit.author_name == target_name:
                        match = True
                    elif commit.author_name == username:
                        match = True
                    
                    if not match:
                        continue
                
                # 獲取 commit 詳細資訊
                try:
                    commit_detail = self.client.get_commit_detail(project.id, commit.id)
                    diff = self.client.get_commit_diff(project.id, commit.id)
                    
                    commit_info = {
                        'project_id': project.id,
                        'project_name': project.name,
                        'commit_id': commit.id,
                        'commit_short_id': commit.short_id,
                        'author_name': commit.author_name,
                        'author_email': commit.author_email,
                        'committed_date': commit.committed_date,
                        'title': commit.title,
                        'message': commit.message,
                        'stats': commit_detail.stats,
                        'diff': diff
                    }
                    
                    user_data['commits'].append(commit_info)
                    
                    # 分析程式碼異動
                    for file_diff in diff:
                        user_data['code_changes'].append({
                            'project_id': project.id,
                            'project_name': project.name,
                            'commit_id': commit.id,
                            'author_name': commit.author_name,
                            'author_email': commit.author_email,
                            'file_path': file_diff.get('new_path') or file_diff.get('old_path'),
                            'old_path': file_diff.get('old_path'),
                            'new_path': file_diff.get('new_path'),
                            'new_file': file_diff.get('new_file'),
                            'renamed_file': file_diff.get('renamed_file'),
                            'deleted_file': file_diff.get('deleted_file'),
                            'diff': file_diff.get('diff', '')
                        })
                except Exception as e:
                    print(f"Warning: Failed to get commit detail for {commit.id}: {e}")
                    continue
            
            # 獲取 Merge Requests
            mrs = self.client.get_project_merge_requests(
                project.id,
                updated_after=start_date,
                updated_before=end_date
            )
            
            for mr in mrs:
                # 改善匹配邏輯：使用 username 匹配
                if username:
                    match = False
                    if target_username and mr.author['username'] == target_username:
                        match = True
                    elif mr.author['username'] == username:
                        match = True
                    
                    if not match:
                        continue
                
                try:
                    mr_detail = self.client.get_merge_request_detail(project.id, mr.iid)
                    discussions = self.client.get_merge_request_discussions(project.id, mr.iid)
                    
                    mr_info = {
                        'project_id': project.id,
                        'project_name': project.name,
                        'mr_iid': mr.iid,
                        'title': mr.title,
                        'state': mr.state,
                        'author': mr.author['username'],
                        'created_at': mr.created_at,
                        'updated_at': mr.updated_at,
                        'merged_at': getattr(mr, 'merged_at', None),
                        'source_branch': mr.source_branch,
                        'target_branch': mr.target_branch,
                        'upvotes': mr.upvotes,
                        'downvotes': mr.downvotes,
                        'discussion_count': len(discussions)
                    }
                    
                    user_data['merge_requests'].append(mr_info)
                    
                    # 分析 Code Review
                    for discussion in discussions:
                        for note in discussion.attributes.get('notes', []):
                            user_data['code_reviews'].append({
                                'project_id': project.id,
                                'project_name': project.name,
                                'mr_iid': mr.iid,
                                'author': note.get('author', {}).get('username', ''),
                                'created_at': note.get('created_at', ''),
                                'body': note.get('body', ''),
                                'type': note.get('type', ''),
                                'resolvable': note.get('resolvable', False),
                                'resolved': note.get('resolved', False)
                            })
                except Exception as e:
                    print(f"Warning: Failed to get MR detail for {mr.iid}: {e}")
                    continue
            
            # 獲取專案授權資訊
            try:
                project_detail = self.client.get_project(project.id)
                members = project_detail.members.list(all=True)
                
                for member in members:
                    # 改善匹配邏輯：使用 username 匹配
                    if username:
                        match = False
                        if target_username and member.username == target_username:
                            match = True
                        elif member.username == username:
                            match = True
                        
                        if not match:
                            continue
                    
                    user_data['permissions'].append({
                        'project_id': project.id,
                        'project_name': project.name,
                        'member_type': 'User',
                        'member_id': member.id,
                        'member_name': member.name,
                        'member_username': member.username,
                        'member_email': getattr(member, 'email', ''),
                        'access_level': member.access_level,
                        'access_level_name': AccessLevelUtil.get_level_name(member.access_level),
                        'expires_at': getattr(member, 'expires_at', None)
                    })
            except Exception as e:
                self.progress.report_warning(f"Failed to get permissions for project {project.name}: {e}")
        
        return user_data


class UserProjectsFetcher(IDataFetcher):
    """使用者專案列表獲取器"""
    
    def __init__(self, client: GitLabClient, progress_reporter: Optional[IProgressReporter] = None):
        self.client = client
        self.progress = progress_reporter or SilentProgressReporter()
    
    def fetch(self, username: Optional[str] = None, group_id: Optional[int] = None) -> Dict[str, Any]:
        """
        獲取使用者參與的專案列表
        
        Args:
            username: 使用者名稱 (可選)
            group_id: 群組 ID (可選)
        
        Returns:
            使用者專案資料
        """
        self.progress.report_start("正在獲取專案列表...")
        projects = self.client.get_projects(group_id=group_id)
        self.progress.report_complete(f"找到 {len(projects)} 個專案")
        
        user_projects = []
        
        # 驗證使用者是否存在
        user_info = None
        if username:
            try:
                users = self.client.gl.users.list(username=username)
                if users:
                    user_info = users[0]
                    self.progress.report_complete(f"找到使用者：{user_info.name} (@{user_info.username})")
            except Exception as e:
                self.progress.report_warning(f"無法驗證使用者: {e}")
        
        if projects:
            self.progress.report_start(f"正在分析 {len(projects)} 個專案的成員資訊...")
        
        for idx, project in enumerate(projects, 1):
            self.progress.report_progress(idx, len(projects), project.name)
            
            try:
                project_detail = self.client.get_project(project.id)
                members = project_detail.members.list(all=True)
                
                for member in members:
                    # 如果指定了使用者名稱，則過濾
                    if username:
                        if user_info and member.username != user_info.username:
                            continue
                        elif not user_info and member.username != username:
                            continue
                    
                    user_projects.append({
                        'user_id': member.id,
                        'username': member.username,
                        'name': member.name,
                        'email': getattr(member, 'email', ''),
                        'project_id': project.id,
                        'project_name': project.name,
                        'project_description': project.description or '',
                        'project_visibility': project.visibility,
                        'project_created_at': project.created_at,
                        'project_last_activity': project.last_activity_at,
                        'access_level': member.access_level,
                        'access_level_name': AccessLevelUtil.get_level_name(member.access_level),
                        'expires_at': getattr(member, 'expires_at', None)
                    })
            except Exception as e:
                self.progress.report_warning(f"Failed to get members for project {project.name}: {e}")
        
        return {'user_projects': user_projects}


class GroupDataFetcher(IDataFetcher):
    """群組資料獲取器（包含子群組、專案、授權資訊）"""
    
    def __init__(self, client: GitLabClient, progress_reporter: Optional[IProgressReporter] = None):
        self.client = client
        self.progress = progress_reporter or SilentProgressReporter()
    
    def fetch(self, group_name: Optional[str] = None) -> Dict[str, Any]:
        """
        獲取群組資料
        
        Args:
            group_name: 群組名稱 (可選，不填則取得全部)
        
        Returns:
            群組資料字典，包含群組資訊、子群組、專案、授權
        """
        self.progress.report_start("正在獲取群組列表...")
        groups = self.client.get_groups(group_name=group_name)
        self.progress.report_complete(f"找到 {len(groups)} 個群組")
        
        groups_data = []
        subgroups_data = []
        projects_data = []
        permissions_data = []
        
        if groups:
            self.progress.report_start(f"正在分析 {len(groups)} 個群組...")
        
        for idx, group in enumerate(groups, 1):
            try:
                self.progress.report_progress(idx, len(groups), group.name)
                
                # 取得完整群組資訊
                group_detail = self.client.get_group(group.id)
                
                # 群組基本資訊
                group_info = {
                    'group_id': group_detail.id,
                    'group_name': group_detail.name,
                    'group_path': group_detail.path,
                    'group_full_path': group_detail.full_path,
                    'description': getattr(group_detail, 'description', ''),
                    'visibility': getattr(group_detail, 'visibility', ''),
                    'created_at': getattr(group_detail, 'created_at', ''),
                    'web_url': getattr(group_detail, 'web_url', ''),
                    'parent_id': getattr(group_detail, 'parent_id', None),
                }
                
                # 取得群組成員
                members = self.client.get_group_members(group_detail.id)
                group_info['total_members'] = len(members)
                group_info['owners'] = len([m for m in members if m.access_level == 50])
                group_info['maintainers'] = len([m for m in members if m.access_level == 40])
                group_info['developers'] = len([m for m in members if m.access_level == 30])
                group_info['reporters'] = len([m for m in members if m.access_level == 20])
                group_info['guests'] = len([m for m in members if m.access_level == 10])
                
                # 群組成員授權資訊
                for member in members:
                    permissions_data.append({
                        'group_id': group_detail.id,
                        'group_name': group_detail.name,
                        'resource_type': 'Group',
                        'member_id': member.id,
                        'member_name': getattr(member, 'name', ''),
                        'member_username': member.username,
                        'member_email': getattr(member, 'email', ''),
                        'access_level': member.access_level,
                        'access_level_name': AccessLevelUtil.get_level_name(member.access_level),
                        'expires_at': getattr(member, 'expires_at', None)
                    })
                
                # 取得子群組
                try:
                    subgroups = self.client.get_group_subgroups(group_detail.id)
                    group_info['subgroups_count'] = len(subgroups)
                    
                    for subgroup in subgroups:
                        subgroups_data.append({
                            'parent_group_id': group_detail.id,
                            'parent_group_name': group_detail.name,
                            'subgroup_id': subgroup.id,
                            'subgroup_name': subgroup.name,
                            'subgroup_path': subgroup.path,
                            'subgroup_full_path': subgroup.full_path,
                            'description': getattr(subgroup, 'description', ''),
                            'visibility': getattr(subgroup, 'visibility', ''),
                            'web_url': getattr(subgroup, 'web_url', ''),
                        })
                except:
                    group_info['subgroups_count'] = 0
                
                # 取得群組專案
                try:
                    projects = self.client.get_group_projects(group_detail.id)
                    group_info['projects_count'] = len(projects)
                    
                    for project in projects:
                        project_info = {
                            'group_id': group_detail.id,
                            'group_name': group_detail.name,
                            'project_id': project.id,
                            'project_name': project.name,
                            'project_path': project.path,
                            'description': getattr(project, 'description', ''),
                            'visibility': getattr(project, 'visibility', ''),
                            'created_at': getattr(project, 'created_at', ''),
                            'last_activity_at': getattr(project, 'last_activity_at', ''),
                            'web_url': getattr(project, 'web_url', ''),
                        }
                        projects_data.append(project_info)
                        
                        # 取得專案成員授權
                        try:
                            project_detail = self.client.get_project(project.id)
                            project_members = project_detail.members.list(all=True)
                            
                            for member in project_members:
                                permissions_data.append({
                                    'group_id': group_detail.id,
                                    'group_name': group_detail.name,
                                    'resource_type': 'Project',
                                    'resource_id': project.id,
                                    'resource_name': project.name,
                                    'member_id': member.id,
                                    'member_name': getattr(member, 'name', ''),
                                    'member_username': member.username,
                                    'member_email': getattr(member, 'email', ''),
                                    'access_level': member.access_level,
                                    'access_level_name': AccessLevelUtil.get_level_name(member.access_level),
                                    'expires_at': getattr(member, 'expires_at', None)
                                })
                            
                            # 取得共享給群組的授權
                            shared_groups = getattr(project_detail, 'shared_with_groups', [])
                            for shared_group in shared_groups:
                                permissions_data.append({
                                    'group_id': group_detail.id,
                                    'group_name': group_detail.name,
                                    'resource_type': 'Project',
                                    'resource_id': project.id,
                                    'resource_name': project.name,
                                    'member_id': shared_group.get('group_id'),
                                    'member_name': shared_group.get('group_name'),
                                    'member_username': '',
                                    'member_email': '',
                                    'access_level': shared_group.get('group_access_level'),
                                    'access_level_name': AccessLevelUtil.get_level_name(shared_group.get('group_access_level')),
                                    'expires_at': shared_group.get('expires_at', None)
                                })
                        except Exception as e:
                            self.progress.report_warning(f"Failed to get permissions for project {project.name}: {e}")
                except:
                    group_info['projects_count'] = 0
                
                groups_data.append(group_info)
                
            except Exception as e:
                self.progress.report_warning(f"Failed to fetch group {group.name}: {e}")
        
        return {
            'groups': groups_data,
            'subgroups': subgroups_data,
            'projects': projects_data,
            'permissions': permissions_data
        }


# ==================== 資料處理器 (單一職責原則) ====================

class ProjectDataProcessor(IDataProcessor):
    """專案資料處理器（包含授權統計）"""
    
    def process(self, data: Dict[str, Any]) -> Dict[str, pd.DataFrame]:
        """
        處理專案資料和授權資訊
        
        Args:
            data: 包含 'projects' 和 'permissions' 的字典
        
        Returns:
            包含 'projects' 和 'permissions' DataFrame 的字典
        """
        projects = data.get('projects', [])
        permissions = data.get('permissions', [])
        
        result = {}
        
        # 處理專案基本資料
        projects_data = []
        for project in projects:
            # 計算該專案的授權統計
            project_perms = [p for p in permissions if p['project_id'] == project.id]
            user_count = len([p for p in project_perms if p['member_type'] == 'User'])
            group_count = len([p for p in project_perms if p['member_type'] == 'Group'])
            
            # 統計各權限等級的人數
            owner_count = len([p for p in project_perms if p['access_level'] == 50])
            maintainer_count = len([p for p in project_perms if p['access_level'] == 40])
            developer_count = len([p for p in project_perms if p['access_level'] == 30])
            reporter_count = len([p for p in project_perms if p['access_level'] == 20])
            guest_count = len([p for p in project_perms if p['access_level'] == 10])
            
            projects_data.append({
                'project_id': project.id,
                'project_name': project.name,
                'description': getattr(project, 'description', ''),
                'visibility': getattr(project, 'visibility', ''),
                'created_at': getattr(project, 'created_at', ''),
                'last_activity_at': getattr(project, 'last_activity_at', ''),
                'default_branch': getattr(project, 'default_branch', ''),
                'ssh_url': getattr(project, 'ssh_url_to_repo', ''),
                'http_url': getattr(project, 'http_url_to_repo', ''),
                'web_url': getattr(project, 'web_url', ''),
                'star_count': getattr(project, 'star_count', 0),
                'forks_count': getattr(project, 'forks_count', 0),
                'open_issues_count': getattr(project, 'open_issues_count', 0),
                # 新增授權統計欄位
                'total_members': user_count + group_count,
                'user_members': user_count,
                'group_members': group_count,
                'owners': owner_count,
                'maintainers': maintainer_count,
                'developers': developer_count,
                'reporters': reporter_count,
                'guests': guest_count,
            })
        
        result['projects'] = pd.DataFrame(projects_data)
        
        # 處理授權詳細資料
        if permissions:
            result['permissions'] = pd.DataFrame(permissions)
        else:
            result['permissions'] = pd.DataFrame()
        
        return result


class ProjectPermissionProcessor(IDataProcessor):
    """專案授權資料處理器"""
    
    def process(self, permissions: List[Dict[str, Any]]) -> pd.DataFrame:
        """處理授權資料"""
        return pd.DataFrame(permissions)


class UserDataProcessor(IDataProcessor):
    """使用者資料處理器"""
    
    def process(self, user_data: Dict[str, Any]) -> Dict[str, pd.DataFrame]:
        """處理使用者資料"""
        result = {}
        
        # 處理 commits
        if user_data['commits']:
            commits_df = pd.DataFrame([{
                'project_id': c['project_id'],
                'project_name': c['project_name'],
                'commit_id': c['commit_id'],
                'commit_short_id': c['commit_short_id'],
                'author_name': c['author_name'],
                'author_email': c['author_email'],
                'committed_date': c['committed_date'],
                'title': c['title'],
                'additions': c['stats'].get('additions', 0),
                'deletions': c['stats'].get('deletions', 0),
                'total': c['stats'].get('total', 0),
            } for c in user_data['commits']])
            result['commits'] = commits_df
        else:
            result['commits'] = pd.DataFrame()
        
        # 處理程式碼異動
        if user_data['code_changes']:
            result['code_changes'] = pd.DataFrame(user_data['code_changes'])
        else:
            result['code_changes'] = pd.DataFrame()
        
        # 處理 Merge Requests
        if user_data['merge_requests']:
            result['merge_requests'] = pd.DataFrame(user_data['merge_requests'])
        else:
            result['merge_requests'] = pd.DataFrame()
        
        # 處理 Code Reviews
        if user_data['code_reviews']:
            result['code_reviews'] = pd.DataFrame(user_data['code_reviews'])
        else:
            result['code_reviews'] = pd.DataFrame()
        
        # 處理授權資訊
        if user_data.get('permissions'):
            result['permissions'] = pd.DataFrame(user_data['permissions'])
        else:
            result['permissions'] = pd.DataFrame()
        
        # 產生統計資料（包含授權統計）
        result['statistics'] = self._generate_statistics(result)
        
        return result
    
    def _generate_statistics(self, data: Dict[str, pd.DataFrame]) -> pd.DataFrame:
        """產生統計資料（包含授權統計）"""
        stats = []
        
        commits_df = data.get('commits', pd.DataFrame())
        mrs_df = data.get('merge_requests', pd.DataFrame())
        reviews_df = data.get('code_reviews', pd.DataFrame())
        changes_df = data.get('code_changes', pd.DataFrame())
        permissions_df = data.get('permissions', pd.DataFrame())
        
        if not commits_df.empty:
            # 按作者統計
            for author in commits_df['author_name'].unique():
                author_commits = commits_df[commits_df['author_name'] == author]
                author_mrs = mrs_df[mrs_df['author'] == author] if not mrs_df.empty else pd.DataFrame()
                
                # 獲取該作者的授權統計
                author_email = author_commits.iloc[0]['author_email']
                author_perms = permissions_df
                if not permissions_df.empty:
                    # 優先使用 email 匹配，其次使用 username 和 name
                    author_perms = permissions_df[
                        (permissions_df['member_email'] == author_email) |  # Email 優先
                        (permissions_df['member_username'] == author) |
                        (permissions_df['member_name'] == author)
                    ]
                
                # 統計授權資訊
                total_projects_with_access = len(author_perms) if not author_perms.empty else 0
                owner_projects = len(author_perms[author_perms['access_level'] == 50]) if not author_perms.empty else 0
                maintainer_projects = len(author_perms[author_perms['access_level'] == 40]) if not author_perms.empty else 0
                developer_projects = len(author_perms[author_perms['access_level'] == 30]) if not author_perms.empty else 0
                reporter_projects = len(author_perms[author_perms['access_level'] == 20]) if not author_perms.empty else 0
                guest_projects = len(author_perms[author_perms['access_level'] == 10]) if not author_perms.empty else 0
                
                stats.append({
                    'author_name': author,
                    'author_email': author_email,
                    'total_commits': len(author_commits),
                    'total_additions': author_commits['additions'].sum(),
                    'total_deletions': author_commits['deletions'].sum(),
                    'total_changes': author_commits['total'].sum(),
                    'avg_changes_per_commit': author_commits['total'].mean(),
                    'total_merge_requests': len(author_mrs),
                    'merged_mrs': len(author_mrs[author_mrs['state'] == 'merged']) if not author_mrs.empty else 0,
                    'projects_contributed': author_commits['project_name'].nunique(),
                    'total_code_reviews': len(reviews_df[reviews_df['author'] == author]) if not reviews_df.empty else 0,
                    'total_files_changed': len(changes_df[changes_df['author_name'] == author]) if not changes_df.empty else 0,
                    # 新增授權統計
                    'total_projects_with_access': total_projects_with_access,
                    'owner_projects': owner_projects,
                    'maintainer_projects': maintainer_projects,
                    'developer_projects': developer_projects,
                    'reporter_projects': reporter_projects,
                    'guest_projects': guest_projects,
                })
        
        return pd.DataFrame(stats)


class UserProjectsProcessor(IDataProcessor):
    """使用者專案資料處理器"""
    
    def process(self, data: Dict[str, Any]) -> Dict[str, pd.DataFrame]:
        """處理使用者專案資料"""
        result = {}
        
        if data['user_projects']:
            result['projects'] = pd.DataFrame(data['user_projects'])
        else:
            result['projects'] = pd.DataFrame()
        
        # 產生統計資料
        result['statistics'] = self._generate_statistics(result['projects'])
        
        return result
    
    def _generate_statistics(self, projects_df: pd.DataFrame) -> pd.DataFrame:
        """產生統計資料"""
        if projects_df.empty:
            return pd.DataFrame()
        
        stats = []
        
        # 按使用者統計
        for username in projects_df['username'].unique():
            user_projects = projects_df[projects_df['username'] == username]
            user_name = user_projects.iloc[0]['name']
            user_email = user_projects.iloc[0]['email']
            
            stats.append({
                'username': username,
                'name': user_name,
                'email': user_email,
                'total_projects': len(user_projects),
                'owner_projects': len(user_projects[user_projects['access_level'] == 50]),
                'maintainer_projects': len(user_projects[user_projects['access_level'] == 40]),
                'developer_projects': len(user_projects[user_projects['access_level'] == 30]),
                'reporter_projects': len(user_projects[user_projects['access_level'] == 20]),
                'guest_projects': len(user_projects[user_projects['access_level'] == 10]),
            })
        
        return pd.DataFrame(stats)


class GroupDataProcessor(IDataProcessor):
    """群組資料處理器"""
    
    def process(self, data: Dict[str, Any]) -> Dict[str, pd.DataFrame]:
        """
        處理群組資料
        
        Args:
            data: 包含 'groups', 'subgroups', 'projects', 'permissions' 的字典
        
        Returns:
            包含多個 DataFrame 的字典
        """
        result = {}
        
        # 處理群組資料
        groups_data = data.get('groups', [])
        if groups_data:
            result['groups'] = pd.DataFrame(groups_data)
        else:
            result['groups'] = pd.DataFrame()
        
        # 處理子群組資料
        subgroups_data = data.get('subgroups', [])
        if subgroups_data:
            result['subgroups'] = pd.DataFrame(subgroups_data)
        else:
            result['subgroups'] = pd.DataFrame()
        
        # 處理專案資料
        projects_data = data.get('projects', [])
        if projects_data:
            result['projects'] = pd.DataFrame(projects_data)
        else:
            result['projects'] = pd.DataFrame()
        
        # 處理授權資料
        permissions_data = data.get('permissions', [])
        if permissions_data:
            result['permissions'] = pd.DataFrame(permissions_data)
        else:
            result['permissions'] = pd.DataFrame()
        
        return result


# ==================== 資料匯出器 (單一職責原則) ====================

class DataExporter(IDataExporter):
    """資料匯出器 - 支援 CSV 和 Markdown"""
    
    def __init__(self, output_dir: str = "./output"):
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
    
    def export(self, df: pd.DataFrame, filename: str) -> None:
        """匯出資料到 CSV 和 Markdown"""
        if df.empty:
            print(f"Warning: No data to export for {filename}")
            return
        
        # 匯出 CSV
        csv_path = self.output_dir / f"{filename}.csv"
        df.to_csv(csv_path, index=False, encoding='utf-8-sig')
        print(f"✓ CSV exported: {csv_path}")
        
        # 匯出 Markdown
        md_path = self.output_dir / f"{filename}.md"
        with open(md_path, 'w', encoding='utf-8') as f:
            f.write(f"# {filename}\n\n")
            f.write(f"Generated at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            f.write(f"Total records: {len(df)}\n\n")
            f.write("## Data\n\n")
            f.write(df.to_markdown(index=False))
        print(f"✓ Markdown exported: {md_path}")


# ==================== 服務層 (開放封閉原則) ====================

class BaseService(ABC):
    """基礎服務類別"""
    
    def __init__(self, fetcher: IDataFetcher, processor: IDataProcessor, exporter: IDataExporter):
        self.fetcher = fetcher
        self.processor = processor
        self.exporter = exporter
    
    @abstractmethod
    def execute(self, **kwargs) -> None:
        """執行服務"""
        pass


class ProjectStatsService(BaseService):
    """專案統計服務（包含授權資訊）"""
    
    def execute(self, project_name: Optional[str] = None, group_id: Optional[int] = None) -> None:
        """執行專案統計"""
        print("=" * 70)
        print("GitLab 專案資訊查詢（包含授權統計）")
        print("=" * 70)
        
        # 獲取資料（包含授權資訊）
        data = self.fetcher.fetch(project_name=project_name, group_id=group_id)
        
        if not data['projects']:
            print("No projects found.")
            return
        
        # 處理資料
        processed_data = self.processor.process(data)
        
        # 匯出專案資料（包含授權統計）
        if project_name:
            base_filename = f"{project_name}-project-stats"
        else:
            base_filename = "all-project-stats"
        
        self.exporter.export(processed_data['projects'], base_filename)
        
        # 匯出授權詳細資料
        if not processed_data['permissions'].empty:
            permission_filename = f"{base_filename}-permissions"
            self.exporter.export(processed_data['permissions'], permission_filename)
            print(f"\n✓ Total permission records: {len(processed_data['permissions'])}")
        
        print(f"✓ Total projects: {len(processed_data['projects'])}")
        print("=" * 70)


class ProjectPermissionService(BaseService):
    """專案授權服務"""
    
    def execute(self, project_name: Optional[str] = None, group_id: Optional[int] = None) -> None:
        """執行專案授權查詢"""
        print("=" * 70)
        print("GitLab 專案授權資訊查詢")
        print("=" * 70)
        
        # 獲取資料
        permissions = self.fetcher.fetch(project_name=project_name, group_id=group_id)
        
        if not permissions:
            print("No permissions found.")
            return
        
        # 處理資料
        df = self.processor.process(permissions)
        
        # 匯出資料
        if project_name:
            filename = f"{project_name}-project-permission"
        else:
            filename = "all-project-permission"
        
        self.exporter.export(df, filename)
        
        print(f"\n✓ Total permission records: {len(df)}")
        print("=" * 70)


class UserStatsService(BaseService):
    """使用者統計服務"""
    
    def execute(self, username: Optional[str] = None,
                project_name: Optional[str] = None,
                start_date: Optional[str] = None,
                end_date: Optional[str] = None,
                group_id: Optional[int] = None) -> None:
        """執行使用者統計"""
        print("=" * 70)
        print("GitLab 使用者資訊查詢")
        print("=" * 70)
        
        # 驗證使用者是否存在
        user_info = None
        if username:
            try:
                users = self.fetcher.client.gl.users.list(username=username)
                if not users:
                    print(f"\n❌ 錯誤：找不到使用者 '{username}'")
                    print("\n建議：")
                    print(f"  • 檢查使用者名稱是否正確")
                    print(f"  • 使用 GitLab username（不是顯示名稱）")
                    print(f"  • 執行不帶 --username 參數查看所有使用者")
                    print("\n" + "=" * 70)
                    return
                else:
                    user_info = users[0]
                    print(f"\n✓ 找到使用者：{user_info.name} (@{user_info.username})")
                    if hasattr(user_info, 'email'):
                        print(f"  Email: {user_info.email}")
            except Exception as e:
                print(f"\n⚠️  警告：無法驗證使用者 ({e})")
                print("  繼續執行查詢...")
        
        # 顯示查詢範圍
        if project_name:
            print(f"\n📂 查詢範圍：專案 '{project_name}'")
        
        # 獲取資料
        user_data = self.fetcher.fetch(
            username=username,
            project_name=project_name,
            start_date=start_date,
            end_date=end_date,
            group_id=group_id,
            user_info=user_info  # 傳遞使用者資訊以便精確匹配
        )
        
        # 處理資料
        processed_data = self.processor.process(user_data)
        
        # 匯出資料
        if username and project_name:
            base_filename = f"{username}-{project_name}-user"
        elif username:
            base_filename = f"{username}-user"
        elif project_name:
            base_filename = f"{project_name}-users"
        else:
            base_filename = "all-users"
        
        # 匯出各類資料並計數
        exported_count = 0
        for data_type, df in processed_data.items():
            if not df.empty:
                filename = f"{base_filename}-{data_type}"
                self.exporter.export(df, filename)
                exported_count += 1
        
        # 顯示統計摘要
        if not processed_data['statistics'].empty:
            print("\n" + "=" * 70)
            print("統計摘要")
            print("=" * 70)
            print(processed_data['statistics'].to_string(index=False))
        
        print("\n" + "=" * 70)
        
        # 檢查是否有輸出資料
        if exported_count == 0:
            if username:
                print(f"⚠️  警告：沒有找到使用者 '{username}' 的任何資料")
                print("\n可能原因：")
                print(f"  • 使用者在指定時間範圍內沒有任何活動")
                print(f"  • 使用者的 Git 設定名稱與 GitLab username 不同")
                print(f"  • 使用者沒有權限存取的專案")
                print(f"\n建議：")
                print(f"  • 嘗試調整時間範圍（--start-date / --end-date）")
                print(f"  • 執行不帶 --username 參數查看所有開發者名稱")
            else:
                print("⚠️  警告：沒有找到任何使用者資料")
        else:
            print("✓ 查詢完成！")
            print(f"✓ 共匯出 {exported_count} 個資料檔案")
        
        print("=" * 70)


class UserProjectsService(BaseService):
    """使用者專案服務"""
    
    def execute(self, username: Optional[str] = None, group_id: Optional[int] = None) -> None:
        """執行使用者專案查詢"""
        print("=" * 70)
        print("GitLab 使用者專案列表查詢")
        print("=" * 70)
        
        # 驗證使用者是否存在
        if username:
            try:
                users = self.fetcher.client.gl.users.list(username=username)
                if not users:
                    print(f"\n❌ 錯誤：找不到使用者 '{username}'")
                    print("\n建議：")
                    print(f"  • 檢查使用者名稱是否正確")
                    print(f"  • 使用 GitLab username（不是顯示名稱）")
                    print(f"  • 執行不帶 --username 參數查看所有使用者")
                    print("\n" + "=" * 70)
                    return
                else:
                    user_info = users[0]
                    print(f"\n✓ 找到使用者：{user_info.name} (@{user_info.username})")
                    if hasattr(user_info, 'email'):
                        print(f"  Email: {user_info.email}")
            except Exception as e:
                print(f"\n⚠️  警告：無法驗證使用者 ({e})")
                print("  繼續執行查詢...")
        
        # 獲取資料
        user_data = self.fetcher.fetch(username=username, group_id=group_id)
        
        # 處理資料
        processed_data = self.processor.process(user_data)
        
        # 匯出資料
        if username:
            base_filename = f"{username}-user_project"
        else:
            base_filename = "all-users_project"
        
        # 匯出各類資料並計數
        exported_count = 0
        for data_type, df in processed_data.items():
            if not df.empty:
                if data_type == 'projects':
                    filename = base_filename
                else:
                    filename = f"{base_filename}-{data_type}"
                self.exporter.export(df, filename)
                exported_count += 1
        
        # 顯示統計摘要
        if not processed_data['statistics'].empty:
            print("\n" + "=" * 70)
            print("統計摘要")
            print("=" * 70)
            print(processed_data['statistics'].to_string(index=False))
        
        print("\n" + "=" * 70)
        
        # 檢查是否有輸出資料
        if exported_count == 0:
            if username:
                print(f"⚠️  警告：沒有找到使用者 '{username}' 的任何專案")
            else:
                print("⚠️  警告：沒有找到任何使用者專案資料")
        else:
            print("✓ 查詢完成！")
            print(f"✓ 共匯出 {exported_count} 個資料檔案")
        
        print("=" * 70)


class GroupStatsService(BaseService):
    """群組統計服務"""
    
    def execute(self, group_name: Optional[str] = None) -> None:
        """執行群組統計"""
        print("=" * 70)
        print("GitLab 群組資訊查詢")
        print("=" * 70)
        
        # 獲取資料
        group_data = self.fetcher.fetch(group_name=group_name)
        
        if not group_data['groups']:
            print("No groups found.")
            return
        
        # 處理資料
        processed_data = self.processor.process(group_data)
        
        # 決定檔名
        if group_name:
            base_filename = f"{group_name}-group-stats"
        else:
            base_filename = "all-groups-stats"
        
        # 匯出群組資料
        if not processed_data['groups'].empty:
            self.exporter.export(processed_data['groups'], base_filename)
            print(f"\n✓ Total groups: {len(processed_data['groups'])}")
        
        # 匯出子群組資料
        if not processed_data['subgroups'].empty:
            subgroups_filename = f"{base_filename}-subgroups"
            self.exporter.export(processed_data['subgroups'], subgroups_filename)
            print(f"✓ Total subgroups: {len(processed_data['subgroups'])}")
        
        # 匯出專案資料
        if not processed_data['projects'].empty:
            projects_filename = f"{base_filename}-projects"
            self.exporter.export(processed_data['projects'], projects_filename)
            print(f"✓ Total projects: {len(processed_data['projects'])}")
        
        # 匯出授權資料
        if not processed_data['permissions'].empty:
            permissions_filename = f"{base_filename}-permissions"
            self.exporter.export(processed_data['permissions'], permissions_filename)
            print(f"✓ Total permission records: {len(processed_data['permissions'])}")
        
        print("=" * 70)


# ==================== CLI 介面 ====================

class GitLabCLI:
    """GitLab CLI 主程式"""
    
    def __init__(self):
        self.client = GitLabClient(
            gitlab_url=config.GITLAB_URL,
            private_token=config.GITLAB_TOKEN,
            ssl_verify=False
        )
        self.exporter = DataExporter(output_dir=config.OUTPUT_DIR)
        self.progress = ConsoleProgressReporter()
    
    def create_project_stats_service(self) -> ProjectStatsService:
        """創建專案統計服務"""
        fetcher = ProjectDataFetcher(self.client, self.progress)
        processor = ProjectDataProcessor()
        return ProjectStatsService(fetcher, processor, self.exporter)
    
    def create_project_permission_service(self) -> ProjectPermissionService:
        """創建專案授權服務"""
        fetcher = ProjectPermissionFetcher(self.client, self.progress)
        processor = ProjectPermissionProcessor()
        return ProjectPermissionService(fetcher, processor, self.exporter)
    
    def create_user_stats_service(self) -> UserStatsService:
        """創建使用者統計服務"""
        fetcher = UserDataFetcher(self.client, self.progress)
        processor = UserDataProcessor()
        return UserStatsService(fetcher, processor, self.exporter)
    
    def create_user_projects_service(self) -> UserProjectsService:
        """創建使用者專案服務"""
        fetcher = UserProjectsFetcher(self.client, self.progress)
        processor = UserProjectsProcessor()
        return UserProjectsService(fetcher, processor, self.exporter)
    
    def create_group_stats_service(self) -> GroupStatsService:
        """創建群組統計服務"""
        fetcher = GroupDataFetcher(self.client, self.progress)
        processor = GroupDataProcessor()
        return GroupStatsService(fetcher, processor, self.exporter)
    
    def run(self):
        """執行 CLI"""
        parser = self._create_parser()
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
    
    def _create_parser(self) -> argparse.ArgumentParser:
        """創建參數解析器"""
        parser = argparse.ArgumentParser(
            description='GitLab 開發者程式碼品質與技術水平分析工具',
            formatter_class=argparse.RawDescriptionHelpFormatter,
            epilog="""
使用範例:

  # 1. 取得所有專案資訊（包含授權統計）
  python gl-cli.py project-stats
  
  # 2. 取得特定專案資訊（包含授權統計）
  python gl-cli.py project-stats --project-name "my-project"
  
  # 3. 取得所有專案授權資訊
  python gl-cli.py project-permission
  
  # 4. 取得特定專案授權資訊
  python gl-cli.py project-permission --project-name "my-project"
  
  # 5. 取得所有使用者詳細資訊（commits, code changes, merge requests, code reviews）
  python gl-cli.py user-stats --start-date 2024-01-01 --end-date 2024-12-31
  
  # 6. 取得特定使用者詳細資訊
  python gl-cli.py user-stats --username johndoe --start-date 2024-01-01
  
  # 7. 取得特定專案的開發者活動
  python gl-cli.py user-stats --project-name "web-api" --start-date 2024-01-01
  
  # 8. 取得所有使用者的專案列表（包含授權資訊）
  python gl-cli.py user-projects
  
  # 9. 取得特定使用者的專案列表（包含授權資訊）
  python gl-cli.py user-projects --username johndoe
  
  # 10. 取得所有群組資訊
  python gl-cli.py group-stats
  
  # 11. 取得特定群組資訊
  python gl-cli.py group-stats --group-name "my-group"
            """
        )
        
        subparsers = parser.add_subparsers(dest='command', help='可用的命令')
        subparsers.required = True
        
        # 1. project-stats 命令
        project_stats_parser = subparsers.add_parser(
            'project-stats',
            help='取得專案所有資訊'
        )
        project_stats_parser.add_argument(
            '--project-name',
            type=str,
            help='專案名稱 (可選，不填則取得全部)'
        )
        project_stats_parser.add_argument(
            '--group-id',
            type=int,
            help=f'群組 ID (預設: {config.TARGET_GROUP_ID})'
        )
        project_stats_parser.set_defaults(func=self._cmd_project_stats)
        
        # 2. project-permission 命令
        project_perm_parser = subparsers.add_parser(
            'project-permission',
            help='取得專案授權資訊'
        )
        project_perm_parser.add_argument(
            '--project-name',
            type=str,
            help='專案名稱 (可選，不填則取得全部)'
        )
        project_perm_parser.add_argument(
            '--group-id',
            type=int,
            help=f'群組 ID (預設: {config.TARGET_GROUP_ID})'
        )
        project_perm_parser.set_defaults(func=self._cmd_project_permission)
        
        # 3. user-stats 命令
        user_stats_parser = subparsers.add_parser(
            'user-stats',
            help='取得使用者資訊'
        )
        user_stats_parser.add_argument(
            '--username',
            type=str,
            help='使用者名稱 (可選，不填則取得全部)'
        )
        user_stats_parser.add_argument(
            '--project-name',
            type=str,
            help='專案名稱 (可選，不填則取得全部)'
        )
        user_stats_parser.add_argument(
            '--start-date',
            type=str,
            help=f'開始日期 (格式: YYYY-MM-DD，預設: {config.START_DATE})'
        )
        user_stats_parser.add_argument(
            '--end-date',
            type=str,
            help=f'結束日期 (格式: YYYY-MM-DD，預設: {config.END_DATE})'
        )
        user_stats_parser.add_argument(
            '--group-id',
            type=int,
            help=f'群組 ID (預設: {config.TARGET_GROUP_ID})'
        )
        user_stats_parser.set_defaults(func=self._cmd_user_stats)
        
        # 4. user-projects 命令
        user_projects_parser = subparsers.add_parser(
            'user-projects',
            help='取得使用者專案列表'
        )
        user_projects_parser.add_argument(
            '--username',
            type=str,
            help='使用者名稱 (可選，不填則取得全部)'
        )
        user_projects_parser.add_argument(
            '--group-id',
            type=int,
            help=f'群組 ID (預設: {config.TARGET_GROUP_ID})'
        )
        user_projects_parser.set_defaults(func=self._cmd_user_projects)
        
        # 5. group-stats 命令
        group_stats_parser = subparsers.add_parser(
            'group-stats',
            help='取得群組所有資訊'
        )
        group_stats_parser.add_argument(
            '--group-name',
            type=str,
            help='群組名稱 (可選，不填則取得全部)'
        )
        group_stats_parser.set_defaults(func=self._cmd_group_stats)
        
        return parser
    
    def _cmd_project_stats(self, args):
        """執行專案統計命令"""
        service = self.create_project_stats_service()
        service.execute(
            project_name=args.project_name,
            group_id=args.group_id or config.TARGET_GROUP_ID
        )
    
    def _cmd_project_permission(self, args):
        """執行專案授權命令"""
        service = self.create_project_permission_service()
        service.execute(
            project_name=args.project_name,
            group_id=args.group_id or config.TARGET_GROUP_ID
        )
    
    def _cmd_user_stats(self, args):
        """執行使用者統計命令"""
        service = self.create_user_stats_service()
        service.execute(
            username=args.username,
            project_name=args.project_name,
            start_date=args.start_date or config.START_DATE,
            end_date=args.end_date or config.END_DATE,
            group_id=args.group_id or config.TARGET_GROUP_ID
        )
    
    def _cmd_user_projects(self, args):
        """執行使用者專案命令"""
        service = self.create_user_projects_service()
        service.execute(
            username=args.username,
            group_id=args.group_id or config.TARGET_GROUP_ID
        )
    
    def _cmd_group_stats(self, args):
        """執行群組統計命令"""
        service = self.create_group_stats_service()
        service.execute(group_name=args.group_name)


def main():
    """主程式入口"""
    cli = GitLabCLI()
    cli.run()


if __name__ == "__main__":
    main()
