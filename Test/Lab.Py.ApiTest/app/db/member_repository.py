from abc import ABC, abstractmethod
from typing import List, Optional

from app.models.member import Member, MemberCreate, UpdateMemberRequest


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
