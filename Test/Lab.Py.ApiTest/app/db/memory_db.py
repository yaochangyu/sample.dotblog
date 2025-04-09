from abc import ABC, abstractmethod
from typing import Dict, List, Optional
from app.models.member import Member, MemberCreate, UpdateMemberRequest
from datetime import datetime


# 定義 Repository 合約（接口）
class MemberRepositoryInterface(ABC):
    @abstractmethod
    def get_all(self) -> List[Member]:
        pass

    @abstractmethod
    def get_by_id(self, member_id: str) -> Optional[Member]:
        pass

    @abstractmethod
    def create(self, member_create: MemberCreate) -> Member:
        pass

    @abstractmethod
    def update(self, member_id: str, update_member_request: UpdateMemberRequest) -> Optional[Member]:
        pass

    @abstractmethod
    def delete(self, member_id: str) -> bool:
        pass


# 內存實現
class MemoryMemberRepository(MemberRepositoryInterface):
    def __init__(self):
        self.members: Dict[str, Member] = {}

    def get_all(self) -> List[Member]:
        return list(self.members.values())

    def get_by_id(self, member_id: str) -> Optional[Member]:
        return self.members.get(member_id)

    def create(self, member_create: MemberCreate) -> Member:
        dump = member_create.model_dump()
        member = Member(**dump)
        self.members[member.id] = member
        return member

    def update(self, member_id: str, update_member_request: UpdateMemberRequest) -> Optional[Member]:
        if member_id not in self.members:
            return None

        current_member = self.members[member_id]
        update_data = update_member_request.model_dump(exclude_unset=True)

        for key, value in update_data.items():
            if value is not None:
                setattr(current_member, key, value)

        return current_member

    def delete(self, member_id: str) -> bool:
        if member_id not in self.members:
            return False

        del self.members[member_id]
        return True


member_memory_repository: MemberRepositoryInterface = MemoryMemberRepository()
