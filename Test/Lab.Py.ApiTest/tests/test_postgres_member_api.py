import pytest
from fastapi.testclient import TestClient
from datetime import date
import uuid
import os
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, declarative_base
from sqlalchemy.ext.declarative import DeclarativeMeta
from dotenv import load_dotenv

load_dotenv()

# 設置測試環境變數
os.environ['USE_POSTGRES'] = 'True'  # 強制使用 PostgreSQL

# 設置測試用的臨時資料庫
TEST_DATABASE_URL = 'postgresql://postgres:postgres@localhost:5432/test_members_db'
os.environ['DATABASE_URL'] = TEST_DATABASE_URL

# 導入被測目標物
from app.main import app

# 創建被測目標的客戶端
client = TestClient(app)


@pytest.fixture(scope="session")
def test_initial():
    from tests.create_test_db import create_test_database, drop_test_database

    """創建並管理測試資料庫的生命週期"""
    print("\nSetting up test database...")
    create_test_database()

    yield

    # 測試結束後刪除整個測試資料庫
    print("\nCleaning up test database...")
    # drop_test_database()


@pytest.fixture(scope="session")
def test_db_engine(test_initial):
    """創建並返回測試資料庫引擎"""
    # 創建測試專用的 SQLAlchemy 引擎
    engine = create_engine(TEST_DATABASE_URL)
    yield engine


@pytest.fixture(scope="session")
def test_db_base(test_db_engine):
    """創建並返回測試資料庫的 Base 類"""
    test_db_base = declarative_base()
    yield test_db_base


@pytest.fixture(scope="session")
def test_db_session_local(test_db_engine):
    """創建並返回測試資料庫的 SessionLocal"""
    test_db_session_local = sessionmaker(autocommit=False, autoflush=False, bind=test_db_engine)
    yield test_db_session_local


@pytest.fixture(scope="module", autouse=True)
def setup_database(test_db_engine):
    """設置測試資料庫環境"""
    # 導入應用程序中定義的 Base
    from app.db.member_postgres_repository import Base, MemberModel

    # 創建所有表格
    Base.metadata.create_all(bind=test_db_engine)
    yield
    # 測試完成後刪除所有表格
    # Base.metadata.drop_all(bind=test_db_engine)


@pytest.fixture(autouse=True)
def clear_db(test_db_session_local):
    """在每次測試前清空會員數據庫"""
    # 導入被測目標物 - 在這裡導入以確保使用正確的環境變數
    from app.db.member_postgres_repository import MemberModel

    with test_db_session_local() as db:
        db.query(MemberModel).delete()
        db.commit()
    yield


class TestPostgresMemberApi:
    """測試 PostgreSQL 會員 API 的類"""

    # 測試數據
    test_member_data = {
        "first_name": "張",
        "last_name": "三",
        "age": 30,
        "address": "台北市信義區101號",
        "birthday": str(date(1993, 5, 15))
    }

    def test_create_member_postgres(self, test_db_session_local):
        """測試創建會員功能 (PostgreSQL)"""
        from app.db.member_postgres_repository import MemberModel

        response = client.post("/api/v1/members", json=self.test_member_data)
        assert response.status_code == 201
        data = response.json()
        assert data["first_name"] == self.test_member_data["first_name"]
        assert data["last_name"] == self.test_member_data["last_name"]
        assert data["age"] == self.test_member_data["age"]
        assert data["address"] == self.test_member_data["address"]
        assert data["birthday"] == self.test_member_data["birthday"]
        assert "id" in data
        assert "created_at" in data
        assert data["created_by"] == "system"

        # 驗證資料確實存儲在 PostgreSQL 中
        with test_db_session_local() as db:
            member = db.query(MemberModel).filter(MemberModel.id == data["id"]).first()
            assert member is not None
            assert member.first_name == self.test_member_data["first_name"]

    def test_get_all_members_postgres(self):
        """測試獲取所有會員功能 (PostgreSQL)"""
        # 先創建兩個會員
        client.post("/api/v1/members", json=self.test_member_data)

        # 修改一些數據以創建第二個會員
        second_member = self.test_member_data.copy()
        second_member["first_name"] = "李"
        second_member["last_name"] = "四"
        client.post("/api/v1/members", json=second_member)

        # 測試獲取所有會員
        response = client.get("/api/v1/members")

        assert response.status_code == 200
        data = response.json()
        assert len(data) == 2

        # 由於 PostgreSQL 的排序可能不同，我們檢查兩個名字都存在
        first_names = [member["first_name"] for member in data]
        assert "張" in first_names
        assert "李" in first_names

    def test_get_member_by_id_postgres(self):
        """測試通過ID獲取會員功能 (PostgreSQL)"""
        # 先創建一個會員
        create_response = client.post("/api/v1/members", json=self.test_member_data)
        member_id = create_response.json()["id"]

        # 測試獲取該會員
        response = client.get(f"/api/v1/members/{member_id}")

        assert response.status_code == 200
        data = response.json()
        assert data["id"] == member_id
        assert data["first_name"] == self.test_member_data["first_name"]

    def test_get_nonexistent_member_postgres(self):
        """測試獲取不存在的會員 (PostgreSQL)"""
        non_existent_id = str(uuid.uuid4())
        response = client.get(f"/api/v1/members/{non_existent_id}")

        assert response.status_code == 404
        assert f"Member with ID {non_existent_id} not found" in response.json()["detail"]

    def test_update_member_postgres(self, test_db_session_local):
        """測試更新會員功能 (PostgreSQL)"""
        from app.db.member_postgres_repository import MemberModel

        # 先創建一個會員
        create_response = client.post("/api/v1/members", json=self.test_member_data)
        member_id = create_response.json()["id"]

        # 更新會員信息
        update_data = {
            "first_name": "王",
            "age": 35
        }
        response = client.put(f"/api/v1/members/{member_id}", json=update_data)

        assert response.status_code == 200
        data = response.json()
        assert data["id"] == member_id
        assert data["first_name"] == update_data["first_name"]  # 已更新
        assert data["last_name"] == self.test_member_data["last_name"]  # 未更新
        assert data["age"] == update_data["age"]  # 已更新

        # 驗證資料確實在 PostgreSQL 中更新
        with test_db_session_local() as db:
            member = db.query(MemberModel).filter(MemberModel.id == member_id).first()
            assert member is not None
            assert member.first_name == update_data["first_name"]
            assert member.age == update_data["age"]

    def test_update_nonexistent_member_postgres(self):
        """測試更新不存在的會員 (PostgreSQL)"""
        non_existent_id = str(uuid.uuid4())
        update_data = {"first_name": "王"}

        response = client.put(f"/api/v1/members/{non_existent_id}", json=update_data)

        assert response.status_code == 404
        assert f"Member with ID {non_existent_id} not found" in response.json()["detail"]

    def test_delete_member_postgres(self, test_db_session_local):
        """測試刪除會員功能 (PostgreSQL)"""
        from app.db.member_postgres_repository import MemberModel

        # 先創建一個會員
        create_response = client.post("/api/v1/members", json=self.test_member_data)
        member_id = create_response.json()["id"]

        # 刪除該會員
        response = client.delete(f"/api/v1/members/{member_id}")

        assert response.status_code == 204

        # 確認會員已被刪除
        get_response = client.get(f"/api/v1/members/{member_id}")
        assert get_response.status_code == 404

        # 驗證資料確實從 PostgreSQL 中刪除
        with test_db_session_local() as db:
            member = db.query(MemberModel).filter(MemberModel.id == member_id).first()
            assert member is None

    def test_delete_nonexistent_member_postgres(self):
        """測試刪除不存在的會員 (PostgreSQL)"""
        non_existent_id = str(uuid.uuid4())
        response = client.delete(f"/api/v1/members/{non_existent_id}")

        assert response.status_code == 404
        assert f"Member with ID {non_existent_id} not found" in response.json()["detail"]
