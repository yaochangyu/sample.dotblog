from dependency_injector import containers, providers
from app.db.member_memory_repository import MemoryMemberRepository
from app.db.member_postgres_repository import PostgresMemberRepository
from dotenv import load_dotenv
from app.config import USE_POSTGRES

load_dotenv()


class Container(containers.DeclarativeContainer):
    config = providers.Configuration()
    # 環境變數配置
    # 如果設置了 USE_MEMORY，強制使用 MemoryMemberRepository
    # 否則根據 USE_POSTGRES 環境變數決定
    # 預設使用 memory repository
    # 當 USE_POSTGRES 為 true 時，才使用 postgres
    member_repository = providers.Singleton(
        PostgresMemberRepository
        if USE_POSTGRES
        else MemoryMemberRepository
    )
