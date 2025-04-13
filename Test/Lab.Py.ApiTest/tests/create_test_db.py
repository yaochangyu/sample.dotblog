#!/usr/bin/env python3
import psycopg2
from psycopg2.extensions import ISOLATION_LEVEL_AUTOCOMMIT


def create_test_database():
    """
    檢查 test_members_db 數據庫是否存在，如果不存在則創建
    """
    try:
        # 連接到 PostgreSQL 服務器
        conn = psycopg2.connect(
            host="localhost",
            user="postgres",
            password="postgres",
            port="5432"
        )
        conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
        cursor = conn.cursor()

        # 檢查數據庫是否存在
        cursor.execute("SELECT 1 FROM pg_database WHERE datname = 'test_members_db'")
        exists = cursor.fetchone()

        if not exists:
            print("Creating test_members_db database...")
            cursor.execute("CREATE DATABASE test_members_db")
            print("test_members_db database created successfully!")
        else:
            print("test_members_db database already exists.")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error: {e}")
        return False

    return True


def drop_test_database():
    """
    刪除 test_members_db 數據庫
    """
    try:
        # 連接到 PostgreSQL 服務器
        conn = psycopg2.connect(
            host="localhost",
            user="postgres",
            password="postgres",
            port="5432"
        )
        conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
        cursor = conn.cursor()

        # 檢查數據庫是否存在
        cursor.execute("SELECT 1 FROM pg_database WHERE datname = 'test_members_db'")
        exists = cursor.fetchone()

        if exists:
            print("Dropping test_members_db database...")
            # 確保沒有活動連接
            cursor.execute("""
                SELECT pg_terminate_backend(pg_stat_activity.pid)
                FROM pg_stat_activity
                WHERE pg_stat_activity.datname = 'test_members_db'
                AND pid <> pg_backend_pid()
            """)
            cursor.execute("DROP DATABASE test_members_db")
            print("test_members_db database dropped successfully!")
        else:
            print("test_members_db database does not exist.")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error dropping database: {e}")
        return False

    return True


if __name__ == "__main__":
    create_test_database()