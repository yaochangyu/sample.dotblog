from testcontainers.postgres import PostgresContainer
import psycopg2

class TestDatabases:
    def test_postgres_connection(self):
        _image = "postgres:latest"
        _user = "postgres"
        _password = "postgres"
        _dbname = "test_db"
        # 啟動 PostgresSQL 容器
        postgres_container = PostgresContainer(
            image=_image,
            user=_user,
            password=_password,
            dbname=_dbname
        )

        try:
            # 啟動容器
            postgres_container.start()

            host = postgres_container.get_container_host_ip()
            port = postgres_container.get_exposed_port(5432)
            # 建立數據庫連接
            conn = psycopg2.connect(
                host=host,
                port=port,
                user=_user,
                password=_password,
                dbname=_dbname
            )
            cursor = conn.cursor()

            # 測試數據庫操作
            cursor.execute("CREATE TABLE test (id serial PRIMARY KEY, name VARCHAR);")
            cursor.execute("INSERT INTO test (name) VALUES (%s)", ("test_name",))
            conn.commit()
            cursor.execute("SELECT * FROM test;")
            result = cursor.fetchone()

            assert result[1] == "test_name"

            # 清理
            cursor.close()
            conn.close()

        finally:
            postgres_container.stop()
