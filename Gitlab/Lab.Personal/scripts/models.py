"""
GitLab 資料模型

定義專案、使用者、Commit、MR 等資料結構
"""

from dataclasses import dataclass, field
from typing import List, Dict, Any, Optional
from datetime import datetime


@dataclass
class Project:
    """專案資料模型"""
    id: int
    name: str
    path: str
    path_with_namespace: str
    web_url: str
    description: str = ""
    default_branch: str = ""
    created_at: str = ""
    last_activity_at: str = ""
    star_count: int = 0
    forks_count: int = 0
    open_issues_count: int = 0
    visibility: str = ""
    creator_id: Optional[int] = None

    @classmethod
    def from_gitlab_object(cls, project) -> 'Project':
        """從 GitLab API 物件建立 Project"""
        return cls(
            id=project.id,
            name=project.name,
            path=project.path,
            path_with_namespace=project.path_with_namespace,
            web_url=project.web_url,
            description=getattr(project, 'description', ''),
            default_branch=getattr(project, 'default_branch', ''),
            created_at=project.created_at,
            last_activity_at=getattr(project, 'last_activity_at', ''),
            star_count=getattr(project, 'star_count', 0),
            forks_count=getattr(project, 'forks_count', 0),
            open_issues_count=getattr(project, 'open_issues_count', 0),
            visibility=getattr(project, 'visibility', ''),
            creator_id=getattr(project, 'creator_id', None)
        )


@dataclass
class User:
    """使用者資料模型"""
    id: int
    username: str
    name: str
    email: str = ""
    state: str = ""
    avatar_url: str = ""
    web_url: str = ""
    created_at: str = ""
    bio: str = ""
    location: str = ""
    public_email: str = ""
    organization: str = ""
    is_admin: bool = False
    can_create_project: bool = False
    last_sign_in_at: str = ""
    confirmed_at: str = ""
    last_activity_on: str = ""

    @classmethod
    def from_gitlab_object(cls, user) -> 'User':
        """從 GitLab API 物件建立 User"""
        return cls(
            id=user.id,
            username=user.username,
            name=user.name,
            email=getattr(user, 'email', ''),
            state=user.state,
            avatar_url=getattr(user, 'avatar_url', ''),
            web_url=user.web_url,
            created_at=getattr(user, 'created_at', ''),
            bio=getattr(user, 'bio', ''),
            location=getattr(user, 'location', ''),
            public_email=getattr(user, 'public_email', ''),
            organization=getattr(user, 'organization', ''),
            is_admin=getattr(user, 'is_admin', False),
            can_create_project=getattr(user, 'can_create_project', False),
            last_sign_in_at=getattr(user, 'last_sign_in_at', ''),
            confirmed_at=getattr(user, 'confirmed_at', ''),
            last_activity_on=getattr(user, 'last_activity_on', '')
        )


@dataclass
class Commit:
    """Commit 資料模型"""
    project_id: int
    project_name: str
    commit_id: str
    commit_short_id: str
    author_name: str
    author_email: str
    committed_date: str
    title: str
    message: str
    additions: int = 0
    deletions: int = 0
    total_changes: int = 0
    parent_ids: List[str] = field(default_factory=list)
    web_url: str = ""
    committer_name: str = ""
    committer_email: str = ""
    authored_date: str = ""
    created_at: str = ""
    changed_files: List[str] = field(default_factory=list)
    changed_files_count: int = 0
    project_path: str = ""


@dataclass
class CodeChange:
    """程式碼異動資料模型"""
    project_id: int
    project_name: str
    commit_id: str
    commit_short_id: str
    author_name: str
    author_email: str
    committed_date: str
    commit_title: str
    file_path: str
    old_path: str = ""
    new_path: str = ""
    new_file: bool = False
    renamed_file: bool = False
    deleted_file: bool = False
    file_extension: str = ""
    a_mode: str = ""
    b_mode: str = ""
    added_lines: int = 0
    removed_lines: int = 0
    diff_content: str = ""
    web_url: str = ""
    project_path: str = ""


@dataclass
class MergeRequest:
    """Merge Request 資料模型"""
    project_id: int
    project_name: str
    mr_iid: int
    mr_id: int
    title: str
    description: str
    author_name: str
    author_username: str
    state: str
    merged: bool
    created_at: str
    updated_at: str
    merged_at: Optional[str] = None
    closed_at: Optional[str] = None
    merged_by: str = ""
    closed_by: str = ""
    assignees: List[str] = field(default_factory=list)
    reviewers: List[str] = field(default_factory=list)
    source_branch: str = ""
    target_branch: str = ""
    source_project_id: Optional[int] = None
    target_project_id: Optional[int] = None
    work_in_progress: bool = False
    draft: bool = False
    upvotes: int = 0
    downvotes: int = 0
    comment_count: int = 0
    user_notes_count: int = 0
    changes_count: int = 0
    approved_by: List[str] = field(default_factory=list)
    has_conflicts: bool = False
    blocking_discussions_resolved: bool = True
    web_url: str = ""
    changed_files: List[str] = field(default_factory=list)
    changed_files_count: int = 0
    commits: List[str] = field(default_factory=list)
    commits_count: int = 0
    discussions: List[Dict[str, Any]] = field(default_factory=list)
    time_stats: Dict[str, Any] = field(default_factory=dict)
    labels: List[str] = field(default_factory=list)
    milestone: str = ""
    project_path: str = ""
    author_email: str = ""
    comments_detail: str = ""


@dataclass
class Statistics:
    """統計資料模型"""
    project_id: Optional[int] = None
    user_email: Optional[str] = None
    user_name: Optional[str] = None
    user_username: Optional[str] = None
    period: Dict[str, Any] = field(default_factory=dict)
    commits: Dict[str, Any] = field(default_factory=dict)
    files: Dict[str, Any] = field(default_factory=dict)
    merge_requests: Dict[str, Any] = field(default_factory=dict)
    productivity: Dict[str, Any] = field(default_factory=dict)
