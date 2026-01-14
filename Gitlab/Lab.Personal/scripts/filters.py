"""
過濾策略

定義不同的資料過濾策略，用於篩選特定開發者或所有開發者的資料
"""

from abc import ABC, abstractmethod
from typing import Any


class FilterStrategy(ABC):
    """過濾策略抽象基類"""
    
    @abstractmethod
    def should_include_commit(self, commit) -> bool:
        """判斷是否應該包含此 commit"""
        pass
    
    @abstractmethod
    def should_include_merge_request(self, mr) -> bool:
        """判斷是否應該包含此 merge request（作為作者）"""
        pass
    
    @abstractmethod
    def should_include_review(self, note_author) -> bool:
        """判斷是否應該包含此評論（作為審查者）"""
        pass
    
    @abstractmethod
    def get_identifier(self) -> str:
        """取得識別字串（用於檔案命名）"""
        pass


class AllDevelopersFilter(FilterStrategy):
    """所有開發者過濾器 - 不過濾任何資料"""
    
    def should_include_commit(self, commit) -> bool:
        return True
    
    def should_include_merge_request(self, mr) -> bool:
        return True
    
    def should_include_review(self, note_author) -> bool:
        return True
    
    def get_identifier(self) -> str:
        return "all-user"


class SpecificDeveloperFilter(FilterStrategy):
    """特定開發者過濾器 - 只保留特定開發者的資料"""
    
    def __init__(self, email: str = None, username: str = None, name: str = None):
        """
        初始化特定開發者過濾器
        
        Args:
            email: 開發者 email
            username: 開發者 username
            name: 開發者名稱
        """
        if not email and not username and not name:
            raise ValueError("至少需要提供 email、username 或 name 其中一個")
        
        self.email = email.lower() if email else None
        self.username = username.lower() if username else None
        self.name = name.lower() if name else None
    
    def should_include_commit(self, commit) -> bool:
        """檢查 commit 是否符合條件"""
        if self.email and hasattr(commit, 'author_email'):
            if commit.author_email.lower() == self.email:
                return True
        
        if self.username and hasattr(commit, 'author_name'):
            if self.username in commit.author_name.lower():
                return True
        
        if self.name and hasattr(commit, 'author_name'):
            if commit.author_name.lower() == self.name:
                return True
        
        return False
    
    def should_include_merge_request(self, mr) -> bool:
        """檢查 MR 是否符合條件（作為作者）"""
        author = mr.author if hasattr(mr, 'author') else {}
        
        if self.email:
            author_email = author.get('email', '').lower() if isinstance(author, dict) else ''
            if author_email == self.email:
                return True
        
        if self.username:
            author_username = author.get('username', '').lower() if isinstance(author, dict) else ''
            if author_username == self.username:
                return True
        
        if self.name:
            author_name = author.get('name', '').lower() if isinstance(author, dict) else ''
            if author_name == self.name:
                return True
        
        return False
    
    def should_include_review(self, note_author: dict) -> bool:
        """檢查評論是否符合條件（作為審查者）"""
        if self.email:
            note_email = note_author.get('email', '').lower()
            if note_email == self.email:
                return True
        
        if self.username:
            note_username = note_author.get('username', '').lower()
            if note_username == self.username:
                return True
        
        if self.name:
            note_name = note_author.get('name', '').lower()
            if note_name == self.name:
                return True
        
        return False
    
    def get_identifier(self) -> str:
        """取得識別字串"""
        identifier = self.email or self.username or self.name
        # 將特殊字元替換為安全的檔名字元
        safe_identifier = identifier.replace('@', '_at_').replace('.', '_').replace(' ', '_')
        return safe_identifier
