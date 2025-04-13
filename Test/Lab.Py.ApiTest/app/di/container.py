from dependency_injector import containers, providers
from app.db.member_memory_repository import MemoryMemberRepository
from app.db.member_postgres_repository import PostgresMemberRepository
from dotenv import load_dotenv
from app.config import USE_POSTGRES

load_dotenv()


class Container(containers.DeclarativeContainer):
    config = providers.Configuration()
    # 環境變數配置

    env = USE_POSTGRES
    member_repository = providers.Singleton(
        PostgresMemberRepository
        if env
        else MemoryMemberRepository
    )
