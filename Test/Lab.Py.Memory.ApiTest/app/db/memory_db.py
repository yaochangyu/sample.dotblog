from typing import Dict, List, Optional
from app.models.member import Member, MemberCreate, MemberUpdate
from datetime import datetime

class MemberDB:
    def __init__(self):
        self.members: Dict[str, Member] = {}
    
    def get_all(self) -> List[Member]:
        return list(self.members.values())
    
    def get_by_id(self, member_id: str) -> Optional[Member]:
        return self.members.get(member_id)
    
    def create(self, member_create: MemberCreate) -> Member:
        member = Member(**member_create.model_dump())
        self.members[member.id] = member
        return member
    
    def update(self, member_id: str, member_update: MemberUpdate) -> Optional[Member]:
        if member_id not in self.members:
            return None
        
        current_member = self.members[member_id]
        update_data = member_update.model_dump(exclude_unset=True)
        
        for key, value in update_data.items():
            if value is not None:
                setattr(current_member, key, value)
        
        return current_member
    
    def delete(self, member_id: str) -> bool:
        if member_id not in self.members:
            return False
        
        del self.members[member_id]
        return True

# 單例模式，確保整個應用程序中只有一個 MemberDB 實例
member_db = MemberDB()