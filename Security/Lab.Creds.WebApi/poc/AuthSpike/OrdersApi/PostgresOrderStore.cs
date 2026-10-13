using System.Data;
using System.Text.Json;
using Npgsql;

namespace AuthSpike.OrdersApi;

/// <summary>
/// PostgreSQL 實作的訂單與去重狀態儲存（23 單）。
/// - 交易與隔離等級：使用預設 Read Committed，不依賴 Serializable。
/// - 去重與租約：以 SELECT ... FOR UPDATE 與 ON CONFLICT 保證同一業務操作只提交一次，租約到期可由新嘗試接手。
/// - 資料庫結構遷移受版本控制，由 ApplyMigrationsAsync 或明確設定開啟時套用。
/// - 連線或資料庫異常時轉為 CommitUnavailable (503 business_commit_unavailable)。
/// </summary>
public sealed class PostgresOrderStore : OrderStore, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresOrderStore(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 3,
            CommandTimeout = 3,
        };
        _dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
    }

    public static async Task ApplyMigrationsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // 使用 advisory lock 確保多 instance 同時啟動時只有一個執行 migration
        await using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_lock(98234123)", connection);
        await lockCmd.ExecuteNonQueryAsync();

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS orders_schema_migrations (
                    version INT PRIMARY KEY,
                    applied_at TIMESTAMPTZ NOT NULL
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM orders_schema_migrations WHERE version = 1) THEN
                        CREATE TABLE IF NOT EXISTS orders (
                            order_id UUID PRIMARY KEY,
                            client_id VARCHAR(128) NOT NULL,
                            order_reference VARCHAR(64) NOT NULL,
                            item VARCHAR(256) NOT NULL,
                            quantity INT NOT NULL,
                            status VARCHAR(32) NOT NULL,
                            created_at TIMESTAMPTZ NOT NULL
                        );
                        CREATE INDEX IF NOT EXISTS idx_orders_client_ref ON orders(client_id, order_reference);

                        CREATE TABLE IF NOT EXISTS idempotency_keys (
                            client_id VARCHAR(128) NOT NULL,
                            key VARCHAR(256) NOT NULL,
                            order_reference VARCHAR(64) NOT NULL,
                            retain_until TIMESTAMPTZ NOT NULL,
                            PRIMARY KEY (client_id, key)
                        );

                        CREATE TABLE IF NOT EXISTS operations (
                            client_id VARCHAR(128) NOT NULL,
                            order_reference VARCHAR(64) NOT NULL,
                            fingerprint TEXT NOT NULL,
                            attempt_id UUID NOT NULL,
                            lease_until TIMESTAMPTZ NOT NULL,
                            order_id UUID NULL,
                            PRIMARY KEY (client_id, order_reference)
                        );

                        INSERT INTO orders_schema_migrations (version, applied_at) VALUES (1, NOW());
                    END IF;
                END $$;
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(98234123)", connection);
            await unlockCmd.ExecuteNonQueryAsync();
        }
    }

    public override int Count
    {
        get
        {
            try
            {
                using var cmd = _dataSource.CreateCommand("SELECT COUNT(*) FROM orders");
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch
            {
                return 0;
            }
        }
    }

    public override int IdempotencyRecordCount
    {
        get
        {
            try
            {
                using var cmd = _dataSource.CreateCommand("SELECT COUNT(*) FROM idempotency_keys");
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch
            {
                return 0;
            }
        }
    }

    public override bool TryGet(Guid orderId, string verifiedClientId, out StoredOrder? order)
    {
        order = null;
        try
        {
            using var cmd = _dataSource.CreateCommand("""
                SELECT client_id, order_reference, item, quantity, status
                FROM orders
                WHERE order_id = @id AND client_id = @client_id
                """);
            cmd.Parameters.AddWithValue("id", orderId);
            cmd.Parameters.AddWithValue("client_id", verifiedClientId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var clientId = reader.GetString(0);
                var orderRef = reader.GetString(1);
                var item = reader.GetString(2);
                var quantity = reader.GetInt32(3);
                var status = reader.GetString(4);
                order = new StoredOrder(clientId, new CreateOrderRequest(orderRef, item, quantity), status);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public override bool TryCancel(Guid orderId, string verifiedClientId, out StoredOrder? order)
    {
        order = null;
        try
        {
            using var connection = _dataSource.OpenConnection();
            using var tx = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            using var selectCmd = new NpgsqlCommand("""
                SELECT client_id, order_reference, item, quantity, status
                FROM orders
                WHERE order_id = @id AND client_id = @client_id
                FOR UPDATE
                """, connection, tx);
            selectCmd.Parameters.AddWithValue("id", orderId);
            selectCmd.Parameters.AddWithValue("client_id", verifiedClientId);

            using var reader = selectCmd.ExecuteReader();
            if (!reader.Read())
            {
                return false;
            }

            var clientId = reader.GetString(0);
            var orderRef = reader.GetString(1);
            var item = reader.GetString(2);
            var quantity = reader.GetInt32(3);
            reader.Close();

            using var updateCmd = new NpgsqlCommand("""
                UPDATE orders SET status = 'cancelled'
                WHERE order_id = @id
                """, connection, tx);
            updateCmd.Parameters.AddWithValue("id", orderId);
            updateCmd.ExecuteNonQueryAsync().GetAwaiter().GetResult();

            tx.Commit();
            order = new StoredOrder(clientId, new CreateOrderRequest(orderRef, item, quantity), "cancelled");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override bool TryGetByOrderReference(string clientId, string orderReference, out StoredOrder? order)
    {
        order = null;
        try
        {
            using var cmd = _dataSource.CreateCommand("""
                SELECT client_id, order_reference, item, quantity, status
                FROM orders
                WHERE client_id = @client_id AND order_reference = @order_ref
                LIMIT 1
                """);
            cmd.Parameters.AddWithValue("client_id", clientId);
            cmd.Parameters.AddWithValue("order_ref", orderReference);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var cId = reader.GetString(0);
                var orderRef = reader.GetString(1);
                var item = reader.GetString(2);
                var quantity = reader.GetInt32(3);
                var status = reader.GetString(4);
                order = new StoredOrder(cId, new CreateOrderRequest(orderRef, item, quantity), status);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public override int CountByOrderReference(string clientId, string orderReference)
    {
        try
        {
            using var cmd = _dataSource.CreateCommand("""
                SELECT COUNT(*) FROM orders
                WHERE client_id = @client_id AND order_reference = @order_ref
                """);
            cmd.Parameters.AddWithValue("client_id", clientId);
            cmd.Parameters.AddWithValue("order_ref", orderReference);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        catch
        {
            return 0;
        }
    }

    public override DateTimeOffset? KeyRetainUntil(string clientId, string idempotencyKey)
    {
        try
        {
            using var cmd = _dataSource.CreateCommand("""
                SELECT retain_until FROM idempotency_keys
                WHERE client_id = @client_id AND key = @key
                """);
            cmd.Parameters.AddWithValue("client_id", clientId);
            cmd.Parameters.AddWithValue("key", idempotencyKey);
            var result = cmd.ExecuteScalar();
            return result is DateTime dt ? new DateTimeOffset(dt, TimeSpan.Zero) : (DateTimeOffset?)result;
        }
        catch
        {
            return null;
        }
    }

    public override async Task<CreateResult> CreateOnceAsync(string clientId, string idempotencyKey, CreateOrderRequest request, DateTimeOffset now)
    {
        var attempt = Guid.NewGuid();
        var fingerprint = ContentFingerprint(request);

        var hold = TakeHold();
        if (hold is not null)
        {
            await hold.WaitAsync();
        }

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            // 清理過期 key
            await using (var pruneCmd = new NpgsqlCommand("DELETE FROM idempotency_keys WHERE retain_until < @now", connection, tx))
            {
                pruneCmd.Parameters.AddWithValue("now", now);
                await pruneCmd.ExecuteNonQueryAsync();
            }

            // 檢查 key conflict
            await using (var keyCmd = new NpgsqlCommand(
                "SELECT order_reference FROM idempotency_keys WHERE client_id = @client_id AND key = @key FOR UPDATE",
                connection, tx))
            {
                keyCmd.Parameters.AddWithValue("client_id", clientId);
                keyCmd.Parameters.AddWithValue("key", idempotencyKey);
                var existingRef = (string?)await keyCmd.ExecuteScalarAsync();
                if (existingRef is not null && existingRef != request.OrderReference)
                {
                    await tx.RollbackAsync();
                    return new CreateResult(CreateOutcome.KeyConflict);
                }
            }

            // 檢查或鎖定 operation
            await using (var opCmd = new NpgsqlCommand(
                "SELECT fingerprint, attempt_id, lease_until, order_id FROM operations WHERE client_id = @client_id AND order_reference = @order_ref FOR UPDATE",
                connection, tx))
            {
                opCmd.Parameters.AddWithValue("client_id", clientId);
                opCmd.Parameters.AddWithValue("order_ref", request.OrderReference);

                using var reader = await opCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var existingFingerprint = reader.GetString(0);
                    var existingAttempt = reader.GetGuid(1);
                    var existingLease = reader.GetFieldValue<DateTimeOffset>(2);
                    var existingOrderId = reader.IsDBNull(3) ? (Guid?)null : reader.GetGuid(3);
                    await reader.CloseAsync();

                    if (existingFingerprint != fingerprint)
                    {
                        await tx.RollbackAsync();
                        return new CreateResult(CreateOutcome.IdentityConflict);
                    }

                    if (existingOrderId.HasValue)
                    {
                        if (!await RecordOrValidateKeyAsync(connection, tx, clientId, idempotencyKey, request.OrderReference, now + IdempotencyKeyRetention))
                        {
                            await tx.RollbackAsync();
                            return new CreateResult(CreateOutcome.KeyConflict);
                        }
                        await tx.CommitAsync();
                        return new CreateResult(CreateOutcome.Replayed, existingOrderId.Value);
                    }

                    if (existingLease > now)
                    {
                        if (!await RecordOrValidateKeyAsync(connection, tx, clientId, idempotencyKey, request.OrderReference, now + IdempotencyKeyRetention))
                        {
                            await tx.RollbackAsync();
                            return new CreateResult(CreateOutcome.KeyConflict);
                        }
                        await tx.CommitAsync();
                        return new CreateResult(CreateOutcome.InProgress);
                    }

                    // 租約已過期，由本次嘗試接手
                    await using var updateOp = new NpgsqlCommand("""
                        UPDATE operations
                        SET attempt_id = @attempt, lease_until = @lease, fingerprint = @fingerprint
                        WHERE client_id = @client_id AND order_reference = @order_ref
                        """, connection, tx);
                    updateOp.Parameters.AddWithValue("attempt", attempt);
                    updateOp.Parameters.AddWithValue("lease", now + ProcessingLease);
                    updateOp.Parameters.AddWithValue("fingerprint", fingerprint);
                    updateOp.Parameters.AddWithValue("client_id", clientId);
                    updateOp.Parameters.AddWithValue("order_ref", request.OrderReference);
                    await updateOp.ExecuteNonQueryAsync();
                }
                else
                {
                    await reader.CloseAsync();
                    // 首次提交：使用 ON CONFLICT 處理併發插入
                    await using var insertOp = new NpgsqlCommand("""
                        INSERT INTO operations (client_id, order_reference, fingerprint, attempt_id, lease_until, order_id)
                        VALUES (@client_id, @order_ref, @fingerprint, @attempt, @lease, NULL)
                        ON CONFLICT (client_id, order_reference) DO NOTHING
                        """, connection, tx);
                    insertOp.Parameters.AddWithValue("client_id", clientId);
                    insertOp.Parameters.AddWithValue("order_ref", request.OrderReference);
                    insertOp.Parameters.AddWithValue("fingerprint", fingerprint);
                    insertOp.Parameters.AddWithValue("attempt", attempt);
                    insertOp.Parameters.AddWithValue("lease", now + ProcessingLease);
                    var inserted = await insertOp.ExecuteNonQueryAsync();
                    if (inserted == 0)
                    {
                        // 併發下已被他人先插入，重新讀取由他人處理中
                        await tx.RollbackAsync();
                        return new CreateResult(CreateOutcome.InProgress);
                    }
                }
            }

            if (!await RecordOrValidateKeyAsync(connection, tx, clientId, idempotencyKey, request.OrderReference, now + IdempotencyKeyRetention))
            {
                await tx.RollbackAsync();
                return new CreateResult(CreateOutcome.KeyConflict);
            }

            // 測試故障注入檢查
            lock (_gate)
            {
                if (TakeFault(CommitFault.CrashBeforeCommit))
                {
                    tx.Commit();
                    throw new SimulatedCommitFailureException();
                }
            }

            var orderId = Guid.NewGuid();
            await using (var insertOrder = new NpgsqlCommand("""
                INSERT INTO orders (order_id, client_id, order_reference, item, quantity, status, created_at)
                VALUES (@id, @client_id, @order_ref, @item, @quantity, 'active', @now)
                """, connection, tx))
            {
                insertOrder.Parameters.AddWithValue("id", orderId);
                insertOrder.Parameters.AddWithValue("client_id", clientId);
                insertOrder.Parameters.AddWithValue("order_ref", request.OrderReference);
                insertOrder.Parameters.AddWithValue("item", request.Item);
                insertOrder.Parameters.AddWithValue("quantity", request.Quantity);
                insertOrder.Parameters.AddWithValue("now", now);
                await insertOrder.ExecuteNonQueryAsync();
            }

            await using (var completeOp = new NpgsqlCommand("""
                UPDATE operations
                SET order_id = @order_id
                WHERE client_id = @client_id AND order_reference = @order_ref AND attempt_id = @attempt
                """, connection, tx))
            {
                completeOp.Parameters.AddWithValue("order_id", orderId);
                completeOp.Parameters.AddWithValue("client_id", clientId);
                completeOp.Parameters.AddWithValue("order_ref", request.OrderReference);
                completeOp.Parameters.AddWithValue("attempt", attempt);
                var affected = await completeOp.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    // 租約已被他人接手，本嘗試放棄
                    await tx.RollbackAsync();
                    return new CreateResult(CreateOutcome.InProgress);
                }
            }

            await tx.CommitAsync();

            lock (_gate)
            {
                if (TakeFault(CommitFault.LoseResponseAfterCommit))
                {
                    throw new SimulatedCommitFailureException();
                }
            }

            return new CreateResult(CreateOutcome.Created, orderId);
        }
        catch (SimulatedCommitFailureException)
        {
            return new CreateResult(CreateOutcome.CommitUnavailable);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostgresOrderStore Error] {ex}");
            return new CreateResult(CreateOutcome.CommitUnavailable);
        }
    }

    public override void ExpireLeasesForTest()
    {
        try
        {
            using var cmd = _dataSource.CreateCommand("UPDATE operations SET lease_until = @expired WHERE order_id IS NULL");
            cmd.Parameters.AddWithValue("expired", DateTimeOffset.UtcNow.AddSeconds(-1));
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // ignored in test
        }
    }

    public override void ExpireKeyRecordsForTest()
    {
        try
        {
            using var cmd = _dataSource.CreateCommand("UPDATE idempotency_keys SET retain_until = @expired");
            cmd.Parameters.AddWithValue("expired", DateTimeOffset.UtcNow.AddSeconds(-1));
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // ignored in test
        }
    }

    private static async Task<bool> RecordOrValidateKeyAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string clientId, string key, string orderRef, DateTimeOffset retainUntil)
    {
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO idempotency_keys (client_id, key, order_reference, retain_until)
            VALUES (@client_id, @key, @order_ref, @retain_until)
            ON CONFLICT (client_id, key) DO UPDATE
            SET retain_until = EXCLUDED.retain_until
            WHERE idempotency_keys.order_reference = EXCLUDED.order_reference
            """, conn, tx);
        cmd.Parameters.AddWithValue("client_id", clientId);
        cmd.Parameters.AddWithValue("key", key);
        cmd.Parameters.AddWithValue("order_ref", orderRef);
        cmd.Parameters.AddWithValue("retain_until", retainUntil);
        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }
}
