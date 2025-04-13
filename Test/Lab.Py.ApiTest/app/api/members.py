from fastapi import APIRouter, HTTPException, status, Depends
from typing import List
from app.models.member import Member, MemberCreate, UpdateMemberRequest
from app.db.member_repository import MemberRepositoryInterface
from fastapi import Request

router = APIRouter(prefix="/members", tags=["members"])

# 從容器取得 member_repository
def get_member_repository(request: Request) -> MemberRepositoryInterface:
    return request.app.state.container.member_repository()

@router.get("", response_model=List[Member])
async def get_all_members(
    member_repository: MemberRepositoryInterface = Depends(get_member_repository)
):
    return member_repository.get_all()

@router.post("", response_model=Member, status_code=status.HTTP_201_CREATED)
async def create_member(
    member_create: MemberCreate,
    member_repository: MemberRepositoryInterface = Depends(get_member_repository)
):
    return member_repository.create(member_create)

@router.get("/{member_id}", response_model=Member)
async def get_member_by_id(
    member_id: str,
    member_repository: MemberRepositoryInterface = Depends(get_member_repository)
):
    member = member_repository.get_by_id(member_id)
    if member is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Member with ID {member_id} not found"
        )
    return member

@router.put("/{member_id}", response_model=Member)
async def update_member(
    member_id: str,
    member_update: UpdateMemberRequest,
    member_repository: MemberRepositoryInterface = Depends(get_member_repository)
):
    member = member_repository.update(member_id, member_update)
    if member is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Member with ID {member_id} not found"
        )
    return member

@router.delete("/{member_id}", status_code=status.HTTP_204_NO_CONTENT)
async def delete_member(
    member_id: str,
    member_repository: MemberRepositoryInterface = Depends(get_member_repository)
):
    success = member_repository.delete(member_id)
    if not success:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Member with ID {member_id} not found"
        )