from fastapi import APIRouter
from app.api.members import router as members_router

api_router = APIRouter(prefix="/api/v1")
api_router.include_router(members_router)