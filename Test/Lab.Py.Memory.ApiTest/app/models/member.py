from datetime import date, datetime
from pydantic import BaseModel, Field
from typing import Optional
import uuid

class MemberBase(BaseModel):
    first_name: str
    last_name: str
    age: Optional[int] = None
    address: Optional[str] = None
    birthday: date

class MemberCreate(MemberBase):
    pass

class UpdateMemberRequest(BaseModel):
    first_name: Optional[str] = None
    last_name: Optional[str] = None
    age: Optional[int] = None
    address: Optional[str] = None
    birthday: Optional[date] = None

class Member(MemberBase):
    id: str = Field(default_factory=lambda: str(uuid.uuid4()))
    created_by: Optional[str] = "system"
    created_at: datetime = Field(default_factory=datetime.now)