import uvicorn
from fastapi import FastAPI
from app.api import api_router
import os

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