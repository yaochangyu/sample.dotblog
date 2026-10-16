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

    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // 使用 advisory lock 確保多 instance 同時啟動時只有一個執行 migration
        await using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_lock(98234123)", connection);
        await lockCmd.ExecuteNonQueryAsync(cancellationToken);

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
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(98234123)", connection);
            await unlockCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public override async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var cmd = _dataSource.CreateCommand("SELECT COUNT(*) FROM orders");
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public override async Task<int> GetIdempotencyRecordCountAsync(CancellationToken cancellationToken = default)
    {
        await using var cmd = _dataSource.CreateCommand("SELECT COUNT(*) FROM idempotency_keys");
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public override async Task<(bool Found, StoredOrder? Order)> TryGetAsync(Guid orderId, string verifiedClientId, CancellationToken cancellationToken = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT client_id, order_reference, item, quantity, status
            FROM orders
            WHERE order_id = @id AND client_id = @client_id
            """);
        cmd.Parameters.AddWithValue("id", orderId);
        cmd.Parameters.AddWithValue("client_id", verifiedClientId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var clientId = reader.GetString(0);
            var orderRef = reader.GetString(1);
            var item = reader.GetString(2);
            var quantity = reader.GetInt32(3);
            var status = reader.GetString(4);
            return (true, new StoredOrder(clientId, new CreateOrderRequest(orderRef, item, quantity), status));
        }
        return (false, null);
    }

    public override async Task<(bool Success, StoredOrder? Order)> TryCancelAsync(Guid orderId, string verifiedClientId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        await using var selectCmd = new NpgsqlCommand("""
            SELECT client_id, order_reference, item, quantity, status
            FROM orders
            WHERE order_id = @id AND client_id = @client_id
            FOR UPDATE
            """, connection, tx);
        selectCmd.Parameters.AddWithValue("id", orderId);
        selectCmd.Parameters.AddWithValue("client_id", verifiedClientId);

        string clientId;
        string orderRef;
        string item;
        int quantity;

        await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, null);
            }

            clientId = reader.GetString(0);
            orderRef = reader.GetString(1);
            item = reader.GetString(2);
            quantity = reader.GetInt32(3);
        }

        await using var updateCmd = new NpgsqlCommand("""
            UPDATE orders SET status = 'cancelled'
            WHERE order_id = @id
            """, connection, tx);
        updateCmd.Parameters.AddWithValue("id", orderId);
        await updateCmd.ExecuteNonQueryAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return (true, new StoredOrder(clientId, new CreateOrderRequest(orderRef, item, quantity), "cancelled"));
    }

    public override async Task<(bool Found, StoredOrder? Order)> TryGetByOrderReferenceAsync(string clientId, string orderReference, CancellationToken cancellationToken = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT client_id, order_reference, item, quantity, status
            FROM orders
            WHERE client_id = @client_id AND order_reference = @order_ref
            LIMIT 1
            """);
        cmd.Parameters.AddWithValue("client_id", clientId);
        cmd.Parameters.AddWithValue("order_ref", orderReference);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var cId = reader.GetString(0);
            var orderRef = reader.GetString(1);
            var item = reader.GetString(2);
            var quantity = reader.GetInt32(3);
            var status = reader.GetString(4);
            return (true, new StoredOrder(cId, new CreateOrderRequest(orderRef, item, quantity), status));
        }
        return (false, null);
    }

    public override async Task<int> CountByOrderReferenceAsync(string clientId, string orderReference, CancellationToken cancellationToken = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT COUNT(*) FROM orders
            WHERE client_id = @client_id AND order_reference = @order_ref
            """);
        cmd.Parameters.AddWithValue("client_id", clientId);
        cmd.Parameters.AddWithValue("order_ref", orderReference);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public override async Task<DateTimeOffset?> KeyRetainUntilAsync(string clientId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT retain_until FROM idempotency_keys
            WHERE client_id = @client_id AND key = @key
            """);
        cmd.Parameters.AddWithValue("client_id", clientId);
        cmd.Parameters.AddWithValue("key", idempotencyKey);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is DateTime dt ? new DateTimeOffset(dt, TimeSpan.Zero) : (DateTimeOffset?)result;
    }

    public override async Task<CreateResult> CreateOnceAsync(string clientId, string idempotencyKey, CreateOrderRequest request, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var attempt = Guid.NewGuid();
        var fingerprint = ContentFingerprint(request);

        var hold = TakeHold();
        if (hold is not null)
        {
            await hold.WaitAsync(cancellationToken);
        }

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            // 清理過期 key
            await using (var pruneCmd = new NpgsqlCommand("DELETE FROM idempotency_keys WHERE retain_until < @now", connection, tx))
            {
                pruneCmd.Parameters.AddWithValue("now", now);
                await pruneCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 檢查 key conflict
            await using (var keyCmd = new NpgsqlCommand(
                "SELECT order_reference FROM idempotency_keys WHERE client_id = @client_id AND key = @key FOR UPDATE",
                connection, tx))
            {
                keyCmd.Parameters.AddWithValue("client_id", clientId);
                keyCmd.Parameters.AddWithValue("key", idempotencyKey);
                var existingRef = (string?)await keyCmd.ExecuteScalarAsync(cancellationToken);
                if (existingRef is not null && existingRef != request.OrderReference)
                {
                    await tx.RollbackAsync(cancellationToken);
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

                using var reader = await opCmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var existingFingerprint = reader.GetString(0);
                    var existingAttempt = reader.GetGuid(1);
                    var existingLease = reader.GetFieldValue<DateTimeOffset>(2);
                    var existingOrderId = reader.IsDBNull(3) ? (Guid?)null : reader.GetGuid(3);
                    await reader.CloseAsync();

                    if (existingFingerprint != fingerprint)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return new CreateResult(CreateOutcome.IdentityConflict);
                    }

                    if (existingOrderId.HasValue)
                    {
                        if (!await RecordOrValidateKeyAsync(connection, tx, clientId, idempotencyKey, request.OrderReference, now + IdempotencyKeyRetention, cancellationToken))
                        {
                            await tx.RollbackAsync(cancellationToken);
                            return new CreateResult(CreateOutcome.KeyConflict);
                        }
                        await tx.CommitAsync(cancellationToken);
                        return new CreateResult(CreateOutcome.Replayed, existingOrderId.Value);
                    }

                    if (existingLease > now)
                    {
                        if (!await RecordOrValidateKeyAsync(connection, tx, clientId, idempotencyKey, request.OrderReference, now + IdempotencyKeyRetention, cancellationToken))
                        {
                            await tx.RollbackAsync(cancellationToken);
                            return new CreateResult(CreateOutcome.KeyConflict);
                        }
                        await tx.CommitAsync(cancellationToken);
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
                    await updateOp.ExecuteNonQueryAsync(cancellationToken);
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
                    var inserted = await insertOp.ExecuteNonQueryAsync(cancellationToken);
                    if (inserted == 0)
                    {
                        // 併發下已被他人先插入，重新讀取由他人處理中
                        await tx.RollbackAsync(cancellationToken);
                        return new CreateResult(CreateOutcome.InProgress);
                    }
                }
            }

            if (!await RecordOrValidateKeyAsync(connection, tx, clientId, idempotencyKey, request.OrderReference, now + IdempotencyKeyRetention, cancellationToken))
            {
                await tx.RollbackAsync(cancellationToken);
                return new CreateResult(CreateOutcome.KeyConflict);
            }

            // 測試故障注入檢查
            bool shouldCrash;
            lock (_gate)
            {
                shouldCrash = TakeFault(CommitFault.CrashBeforeCommit);
            }
            if (shouldCrash)
            {
                await tx.CommitAsync(cancellationToken);
                throw new SimulatedCommitFailureException();
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
                await insertOrder.ExecuteNonQueryAsync(cancellationToken);
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
                var affected = await completeOp.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0)
                {
                    // 租約已被他人接手，本嘗試放棄
                    await tx.RollbackAsync(cancellationToken);
                    return new CreateResult(CreateOutcome.InProgress);
                }
            }

            await tx.CommitAsync(cancellationToken);

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

    public override async Task ExpireLeasesForTestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var cmd = _dataSource.CreateCommand("UPDATE operations SET lease_until = @expired WHERE order_id IS NULL");
            cmd.Parameters.AddWithValue("expired", DateTimeOffset.UtcNow.AddSeconds(-1));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // ignored in test
        }
    }

    public override async Task ExpireKeyRecordsForTestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var cmd = _dataSource.CreateCommand("UPDATE idempotency_keys SET retain_until = @expired");
            cmd.Parameters.AddWithValue("expired", DateTimeOffset.UtcNow.AddSeconds(-1));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // ignored in test
        }
    }

    private static async Task<bool> RecordOrValidateKeyAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string clientId, string key, string orderRef, DateTimeOffset retainUntil, CancellationToken cancellationToken = default)
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
        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }
}
