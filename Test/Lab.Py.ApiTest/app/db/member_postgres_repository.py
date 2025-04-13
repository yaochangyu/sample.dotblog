from typing import List, Optional
from datetime import datetime
import uuid
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

from app.models.member import Member, MemberCreate, UpdateMemberRequest
from app.db.member_repository import MemberRepositoryInterface
from app.config import DATABASE_URL
from app.db.member_entity import MemberModel  # 從 entities/member_entity.py 導入 MemberModel

engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# 創建數據庫表格（如果不存在）
# Base.metadata.create_all(bind=engine)


class PostgresMemberRepository(MemberRepositoryInterface):
    def get_all(self) -> List[Member]:
        with SessionLocal() as db:
            members = db.query(MemberModel).all()
            return [self._convert_to_member(member) for member in members]

    def get_by_id(self, member_id: str) -> Optional[Member]:
        with SessionLocal() as db:
            member = db.query(MemberModel).filter(MemberModel.id == member_id).first()
            if member:
                return self._convert_to_member(member)
            return None

    def create(self, member_create: MemberCreate) -> Member:
        member_id = str(uuid.uuid4())
        member_data = member_create.model_dump()

        db_member = MemberModel(
            id=member_id,
            first_name=member_data["first_name"],
            last_name=member_data["last_name"],
            age=member_data.get("age"),
            address=member_data.get("address"),
            birthday=member_data["birthday"],
            created_at=datetime.now(),
            created_by="system"
        )

        with SessionLocal() as db:
            db.add(db_member)
            db.commit()
            db.refresh(db_member)

        return self._convert_to_member(db_member)

    def update(self, member_id: str, update_member_request: UpdateMemberRequest) -> Optional[Member]:
        update_data = update_member_request.model_dump(exclude_unset=True)

        if not update_data:
            # 如果沒有要更新的數據，直接返回當前會員
            return self.get_by_id(member_id)

        with SessionLocal() as db:
            member = db.query(MemberModel).filter(MemberModel.id == member_id).first()
            if not member:
                return None

            # 更新會員數據
            for key, value in update_data.items():
                if value is not None:
                    setattr(member, key, value)

            db.commit()
            db.refresh(member)

            return self._convert_to_member(member)

    def delete(self, member_id: str) -> bool:
        with SessionLocal() as db:
            member = db.query(MemberModel).filter(MemberModel.id == member_id).first()
            if not member:
                return False

            db.delete(member)
            db.commit()
            return True

    def _convert_to_member(self, db_member: MemberModel) -> Member:
        """將 SQLAlchemy 模型轉換為 Pydantic 模型"""
        return Member(
            id=db_member.id,
            first_name=db_member.first_name,
            last_name=db_member.last_name,
            age=db_member.age,
            address=db_member.address,
            birthday=db_member.birthday,
            created_by=db_member.created_by,
            created_at=db_member.created_at
        )