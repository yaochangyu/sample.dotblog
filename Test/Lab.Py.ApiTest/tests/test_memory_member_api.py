import pytest
from fastapi.testclient import TestClient
from datetime import date
import uuid
import os

# 設置測試環境變數
os.environ['USE_POSTGRES'] = 'false'  # 強制使用 MemoryMemberRepository

from app.main import app
from app.db.member_memory_repository import MemoryMemberRepository


class TestMemoryMemberApi:
    # 測試數據
    test_member_data = {
        "first_name": "張",
        "last_name": "三",
        "age": 30,
        "address": "台北市信義區101號",
        "birthday": str(date(1993, 5, 15))
    }

    @pytest.fixture(autouse=True)
    def setup(self):
        """在每次測試前後清空會員數據庫"""
        self.client = TestClient(app)
        MemoryMemberRepository.members = {}
        yield
        MemoryMemberRepository.members = {}

    def test_create_member(self):
        """測試創建會員功能"""
        response = self.client.post("/api/v1/members", json=self.test_member_data)
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

    def test_get_all_members(self):
        """測試獲取所有會員功能"""
        # 先創建兩個會員
        self.client.post("/api/v1/members", json=self.test_member_data)

        # 修改一些數據以創建第二個會員
        second_member = self.test_member_data.copy()
        second_member["first_name"] = "李"
        second_member["last_name"] = "四"
        self.client.post("/api/v1/members", json=second_member)

        # 測試獲取所有會員
        response = self.client.get("/api/v1/members")

        assert response.status_code == 200
        data = response.json()
        assert len(data) == 2
        assert data[0]["first_name"] == self.test_member_data["first_name"]
        assert data[1]["first_name"] == second_member["first_name"]

    def test_get_member_by_id(self):
        """測試通過ID獲取會員功能"""
        # 先創建一個會員
        create_response = self.client.post("/api/v1/members", json=self.test_member_data)
        member_id = create_response.json()["id"]

        # 測試獲取該會員
        response = self.client.get(f"/api/v1/members/{member_id}")

        assert response.status_code == 200
        data = response.json()
        assert data["id"] == member_id
        assert data["first_name"] == self.test_member_data["first_name"]

    def test_get_nonexistent_member(self):
        """測試獲取不存在的會員"""
        non_existent_id = str(uuid.uuid4())
        response = self.client.get(f"/api/v1/members/{non_existent_id}")

        assert response.status_code == 404
        assert f"Member with ID {non_existent_id} not found" in response.json()["detail"]

    def test_update_member(self):
        """測試更新會員功能"""
        # 先創建一個會員
        create_response = self.client.post("/api/v1/members", json=self.test_member_data)
        member_id = create_response.json()["id"]

        # 更新會員信息
        update_data = {
            "first_name": "王",
            "age": 35
        }
        response = self.client.put(f"/api/v1/members/{member_id}", json=update_data)

        assert response.status_code == 200
        data = response.json()
        assert data["id"] == member_id
        assert data["first_name"] == update_data["first_name"]  # 已更新
        assert data["last_name"] == self.test_member_data["last_name"]  # 未更新
        assert data["age"] == update_data["age"]  # 已更新

    def test_update_nonexistent_member(self):
        """測試更新不存在的會員"""
        non_existent_id = str(uuid.uuid4())
        update_data = {"first_name": "王"}

        response = self.client.put(f"/api/v1/members/{non_existent_id}", json=update_data)

        assert response.status_code == 404
        assert f"Member with ID {non_existent_id} not found" in response.json()["detail"]

    def test_delete_member(self):
        """測試刪除會員功能"""
        # 先創建一個會員
        create_response = self.client.post("/api/v1/members", json=self.test_member_data)
        member_id = create_response.json()["id"]

        # 刪除該會員
        response = self.client.delete(f"/api/v1/members/{member_id}")

        assert response.status_code == 204

        # 確認會員已被刪除
        get_response = self.client.get(f"/api/v1/members/{member_id}")
        assert get_response.status_code == 404

    def test_delete_nonexistent_member(self):
        """測試刪除不存在的會員"""
        non_existent_id = str(uuid.uuid4())
        response = self.client.delete(f"/api/v1/members/{non_existent_id}")

        assert response.status_code == 404
        assert f"Member with ID {non_existent_id} not found" in response.json()["detail"]