from dependency_injector import containers, providers
from app.db.member_memory_repository import MemoryMemberRepository
from app.db.member_postgres_repository import PostgresMemberRepository
from dotenv import load_dotenv
from app.config import USE_POSTGRES
import os

load_dotenv()


class Container(containers.DeclarativeContainer):
    config = providers.Configuration()
    # 環境變數配置

    # 檢查是否在測試環境中運行
    is_testing = os.environ.get('TESTING', 'False').lower() == 'true'

    # 如果是測試環境，強制使用 MemoryMemberRepository
    # 否則根據 USE_POSTGRES 環境變數決定
    env = False if is_testing else USE_POSTGRES
    member_repository = providers.Singleton(
        PostgresMemberRepository
        if env
        else MemoryMemberRepository
    )
