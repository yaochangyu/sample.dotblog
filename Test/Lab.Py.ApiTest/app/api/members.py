from fastapi import APIRouter, HTTPException, status
from typing import List
from app.models.member import Member, MemberCreate, UpdateMemberRequest
from app.db.memory_db import member_memory_repository
import os
from dotenv import load_dotenv

# 讀取環境變數
load_dotenv()

# 依據環境變數決定使用哪個 repository，預設使用 MemoryMemberRepository
if os.getenv("USE_POSTGRES", "false").lower() == "true":
    from app.db.postgres_db import member_db_repository
    member_repository = member_db_repository
else:
    from app.db.memory_db import member_memory_repository
    member_repository = member_memory_repository

router = APIRouter(prefix="/members", tags=["members"])

@router.get("", response_model=List[Member])
async def get_all_members():
    return member_repository.get_all()

@router.post("", response_model=Member, status_code=status.HTTP_201_CREATED)
async def create_member(member_create: MemberCreate):
    return member_repository.create(member_create)

@router.get("/{member_id}", response_model=Member)
async def get_member_by_id(member_id: str):
    member = member_repository.get_by_id(member_id)
    if member is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Member with ID {member_id} not found"
        )
    return member

@router.put("/{member_id}", response_model=Member)
async def update_member(member_id: str, member_update: UpdateMemberRequest):
    member = member_repository.update(member_id, member_update)
    if member is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Member with ID {member_id} not found"
        )
    return member

@router.delete("/{member_id}", status_code=status.HTTP_204_NO_CONTENT)
async def delete_member(member_id: str):
    success = member_repository.delete(member_id)
    if not success:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Member with ID {member_id} not found"
        )