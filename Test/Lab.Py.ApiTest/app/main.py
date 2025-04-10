import uvicorn
from fastapi import FastAPI
from app.api import api_router
from dotenv import load_dotenv
import os
from app.db.member_memory_repository import member_memory_repository
from app.db.member_repository import MemberRepositoryInterface

# 讀取環境變數
# load_dotenv()

# # 根據環境變數決定使用哪個 repository
# if os.getenv("USE_POSTGRES", "false").lower() == "true":
#     from app.db.postgres_db import postgres_member_repository
#
#     # 全局替換 repository 實例
#     member_db_repository = postgres_member_repository
app = FastAPI(
    title="Member API",
    description="RESTful API for managing members with Memory DB",
    version="0.1.0"
)

app.include_router(api_router)


@app.get("/")
async def root():
    return {"message": "Welcome to Member API. Go to /docs for the API documentation."}


def start():
    """Entry point for the application script"""
    uvicorn.run("app.main:app", host="0.0.0.0", port=8001, reload=True)


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8001)
