from typing import List, Optional
from datetime import datetime, date
from sqlalchemy import create_engine, Column, String, Integer, Date, DateTime
from sqlalchemy.orm import declarative_base

Base = declarative_base()
class MemberModel(Base):
    __tablename__ = "members"

    id = Column(String, primary_key=True, index=True)
    first_name = Column(String, nullable=False)
    last_name = Column(String, nullable=False)
    age = Column(Integer, nullable=True)
    address = Column(String, nullable=True)
    birthday = Column(Date, nullable=False)
    created_by = Column(String, default="system")
    created_at = Column(DateTime, default=datetime.now)

