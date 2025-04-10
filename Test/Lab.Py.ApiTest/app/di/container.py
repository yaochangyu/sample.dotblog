from dependency_injector import containers, providers
from app.db.member_memory_repository import MemoryMemberRepository
from app.db.member_postgres_repository import PostgresMemberRepository
import os
from dotenv import load_dotenv

load_dotenv()


class Container(containers.DeclarativeContainer):
    config = providers.Configuration()

    member_repository = providers.Singleton(
        MemoryMemberRepository
        if os.getenv("USE_POSTGRES", "false").lower() != "true"
        else PostgresMemberRepository
    )
