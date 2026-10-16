using Npgsql;

namespace AuthSpike.Data;

/// <summary>
/// 授權伺服器 PostgreSQL 資料庫結構遷移（受版本控制）。
/// 包含 OpenIddict 資料表與授權伺服器信任名單、申請、持有證明 nonce 與管理操作稽核。
/// 使用 advisory lock 保證多執行個體啟動時僅有一個套用遷移。
/// </summary>
public static class AuthDbMigrations
{
    private const string MigrationV1Sql = """
        -- OpenIddict 資料表（版本控制與可審查 DDL）
        CREATE TABLE IF NOT EXISTS "OpenIddictApplications" (
            "Id" text NOT NULL,
            "ApplicationType" character varying(50),
            "ClientId" character varying(100),
            "ClientSecret" text,
            "ClientType" character varying(50),
            "ConcurrencyToken" character varying(50),
            "ConsentType" character varying(50),
            "DisplayName" text,
            "DisplayNames" text,
            "JsonWebKeySet" text,
            "Permissions" text,
            "PostLogoutRedirectUris" text,
            "Properties" text,
            "RedirectUris" text,
            "Requirements" text,
            "Settings" text,
            CONSTRAINT "PK_OpenIddictApplications" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "OpenIddictScopes" (
            "Id" text NOT NULL,
            "ConcurrencyToken" character varying(50),
            "Description" text,
            "Descriptions" text,
            "DisplayName" text,
            "DisplayNames" text,
            "Name" character varying(200),
            "Properties" text,
            "Resources" text,
            CONSTRAINT "PK_OpenIddictScopes" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "OpenIddictAuthorizations" (
            "Id" text NOT NULL,
            "ApplicationId" text,
            "ConcurrencyToken" character varying(50),
            "CreationDate" timestamp with time zone,
            "Properties" text,
            "Scopes" text,
            "Status" character varying(50),
            "Subject" character varying(400),
            "Type" character varying(50),
            CONSTRAINT "PK_OpenIddictAuthorizations" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_OpenIddictAuthorizations_OpenIddictApplications_Application~" FOREIGN KEY ("ApplicationId") REFERENCES "OpenIddictApplications" ("Id")
        );

        CREATE TABLE IF NOT EXISTS "OpenIddictTokens" (
            "Id" text NOT NULL,
            "ApplicationId" text,
            "AuthorizationId" text,
            "ConcurrencyToken" character varying(50),
            "CreationDate" timestamp with time zone,
            "ExpirationDate" timestamp with time zone,
            "Payload" text,
            "Properties" text,
            "RedemptionDate" timestamp with time zone,
            "ReferenceId" character varying(100),
            "Status" character varying(50),
            "Subject" character varying(400),
            "Type" character varying(150),
            CONSTRAINT "PK_OpenIddictTokens" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "OpenIddictApplications" ("Id"),
            CONSTRAINT "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId" FOREIGN KEY ("AuthorizationId") REFERENCES "OpenIddictAuthorizations" ("Id")
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_OpenIddictApplications_ClientId" ON "OpenIddictApplications" ("ClientId");
        CREATE INDEX IF NOT EXISTS "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type" ON "OpenIddictAuthorizations" ("ApplicationId", "Status", "Subject", "Type");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_OpenIddictScopes_Name" ON "OpenIddictScopes" ("Name");
        CREATE INDEX IF NOT EXISTS "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type" ON "OpenIddictTokens" ("ApplicationId", "Status", "Subject", "Type");
        CREATE INDEX IF NOT EXISTS "IX_OpenIddictTokens_AuthorizationId" ON "OpenIddictTokens" ("AuthorizationId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_OpenIddictTokens_ReferenceId" ON "OpenIddictTokens" ("ReferenceId");

        -- 授權伺服器信任名單、申請、Nonce、稽核與管理員資料表
        CREATE TABLE IF NOT EXISTS auth_clients (
            client_id VARCHAR(100) PRIMARY KEY,
            enabled BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );

        CREATE TABLE IF NOT EXISTS auth_certificates (
            thumbprint VARCHAR(100) PRIMARY KEY,
            client_id VARCHAR(100) NOT NULL REFERENCES auth_clients(client_id),
            status VARCHAR(20) NOT NULL,
            raw_cert TEXT NOT NULL,
            approved_at TIMESTAMPTZ NOT NULL,
            retired_at TIMESTAMPTZ,
            revoked_at TIMESTAMPTZ
        );

        CREATE TABLE IF NOT EXISTS auth_signing_keys (
            key_id VARCHAR(100) PRIMARY KEY,
            client_id VARCHAR(100) NOT NULL REFERENCES auth_clients(client_id),
            status VARCHAR(20) NOT NULL,
            jwk TEXT NOT NULL,
            fingerprint VARCHAR(100),
            approved_at TIMESTAMPTZ NOT NULL,
            retired_at TIMESTAMPTZ,
            revoked_at TIMESTAMPTZ
        );
        ALTER TABLE auth_signing_keys ADD COLUMN IF NOT EXISTS fingerprint VARCHAR(100);

        CREATE TABLE IF NOT EXISTS auth_registration_requests (
            request_id VARCHAR(100) PRIMARY KEY,
            client_id VARCHAR(100) NOT NULL REFERENCES auth_clients(client_id),
            type VARCHAR(20) NOT NULL,
            key_id VARCHAR(100),
            status VARCHAR(20) NOT NULL,
            payload TEXT NOT NULL,
            fingerprint VARCHAR(100),
            applicant_thumbprint VARCHAR(100),
            expires_at TIMESTAMPTZ NOT NULL,
            submitted_at TIMESTAMPTZ NOT NULL,
            decided_at TIMESTAMPTZ
        );
        ALTER TABLE auth_registration_requests ADD COLUMN IF NOT EXISTS key_id VARCHAR(100);

        CREATE TABLE IF NOT EXISTS auth_pop_nonces (
            client_id VARCHAR(100) NOT NULL,
            nonce VARCHAR(100) NOT NULL,
            created_at TIMESTAMPTZ NOT NULL,
            retain_until TIMESTAMPTZ NOT NULL,
            PRIMARY KEY (client_id, nonce)
        );

        CREATE TABLE IF NOT EXISTS auth_administrators (
            thumbprint VARCHAR(100) PRIMARY KEY,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );

        CREATE TABLE IF NOT EXISTS auth_administrative_audits (
            id BIGSERIAL PRIMARY KEY,
            correlation_id VARCHAR(100),
            occurred_at TIMESTAMPTZ NOT NULL,
            operation VARCHAR(100) NOT NULL,
            actor_status VARCHAR(50) NOT NULL,
            actor_thumbprint VARCHAR(100),
            client_id VARCHAR(100),
            subject VARCHAR(50),
            key_id VARCHAR(100),
            fingerprint VARCHAR(100),
            outcome VARCHAR(20) NOT NULL,
            reason TEXT,
            result_status INT NOT NULL,
            applicant_authenticated BOOLEAN NULL,
            applicant_client_id VARCHAR(100)
        );

        CREATE INDEX IF NOT EXISTS idx_auth_req_client_pending 
            ON auth_registration_requests (client_id, status, expires_at);
        CREATE INDEX IF NOT EXISTS idx_auth_pop_nonces_retain 
            ON auth_pop_nonces (retain_until);
    """;

    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // 使用 advisory lock 確保多 instance 同時啟動時只有一個執行 migration
        await using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_lock(98234125)", connection);
        await lockCmd.ExecuteNonQueryAsync(cancellationToken);

        try
        {
            // 1. 版本控制資料表
            await using (var cmd = new NpgsqlCommand("""
                CREATE TABLE IF NOT EXISTS auth_schema_migrations (
                    version INT PRIMARY KEY,
                    applied_at TIMESTAMPTZ NOT NULL
                );
            """, connection))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 檢查版本 1 是否已套用
            bool v1Applied;
            await using (var chkCmd = new NpgsqlCommand("SELECT 1 FROM auth_schema_migrations WHERE version = 1;", connection))
            {
                v1Applied = await chkCmd.ExecuteScalarAsync(cancellationToken) is not null;
            }

            if (!v1Applied)
            {
                // 建立 OpenIddict 與自訂授權伺服器資料表
                await using (var cmd = new NpgsqlCommand(MigrationV1Sql, connection))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // 記錄版本 1 套用完成
                await using (var recCmd = new NpgsqlCommand("INSERT INTO auth_schema_migrations (version, applied_at) VALUES (1, NOW());", connection))
                {
                    await recCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await using (var alterCmd = new NpgsqlCommand("ALTER TABLE auth_administrative_audits ALTER COLUMN reason TYPE TEXT;", connection))
            {
                await alterCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(98234125)", connection);
            await unlockCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
