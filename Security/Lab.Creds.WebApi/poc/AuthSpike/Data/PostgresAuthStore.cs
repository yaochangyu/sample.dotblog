using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AuthSpike.Audit;
using AuthSpike.AuthServer;
using AuthSpike.Certificates;
using AuthSpike.Registration;
using AuthSpike.Signing;
using AuthSpike.Trust;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenIddict.Abstractions;

namespace AuthSpike.Data;

public enum RequestSubmissionOutcome
{
    Accepted,
    DuplicateNonce,
    LimitReached,
    InvalidData,
}

public enum DecisionOutcome
{
    Approved,
    Rejected,
    NotFound,
    NotPending,
    AlreadyRegistered,
}

public record RegistrationRequestRecord(
    string RequestId,
    string ClientId,
    string Fingerprint,
    string Status,
    string ApplicantThumbprint,
    DateTimeOffset ExpiresAt,
    string? KeyId);

public sealed class PostgresAuthStore(string connectionString)
{
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

    /// <summary>每個 Client 的待核准申請數上限（AC-38）。</summary>
    public const int PendingRequestLimitPerClient = 5;

    public async Task InitializeOrLoadClientsAsync(
        IReadOnlyList<AuthServerClient> initialClients,
        TrustRegistry registry,
        VerificationKeyStore verificationKeys,
        Dictionary<string, List<X509Certificate2>> trustedCertificates,
        Func<NpgsqlTransaction, CancellationToken, Task>? onInitializing = null,
        string? initialAdministratorThumbprint = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. 初始化預設 Clients、憑證與管理員（於單一交易內完成，含 OpenIddict application 建立）
        await using (var tx = await connection.BeginTransactionAsync(cancellationToken))
        {
            foreach (var client in initialClients)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO auth_clients (client_id, enabled) VALUES (@id, true) ON CONFLICT (client_id) DO NOTHING;",
                    connection, tx);
                cmd.Parameters.AddWithValue("id", client.ClientId);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // 若 auth_certificates 尚未有該 Client 的初使憑證，寫入
                var cert = client.PublicCertificate;
                var thumbprint = cert.Thumbprint;
                var raw = cert.ExportCertificatePem();
                await using var certCmd = new NpgsqlCommand("""
                    INSERT INTO auth_certificates (thumbprint, client_id, status, raw_cert, approved_at)
                    VALUES (@thumbprint, @clientId, 'approved', @raw, NOW())
                    ON CONFLICT (thumbprint) DO NOTHING;
                """, connection, tx);
                certCmd.Parameters.AddWithValue("thumbprint", thumbprint);
                certCmd.Parameters.AddWithValue("clientId", client.ClientId);
                certCmd.Parameters.AddWithValue("raw", raw);
                await certCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var (clientId, keys) in verificationKeys.Snapshot())
            {
                foreach (var key in keys)
                {
                    var parameters = key.Key.ExportParameters(false);
                    var jwk = new JsonWebKey
                    {
                        Kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
                        Crv = JsonWebKeyECTypes.P256,
                        X = Base64UrlEncoder.Encode(parameters.Q.X),
                        Y = Base64UrlEncoder.Encode(parameters.Q.Y),
                        Kid = key.KeyId,
                    };
                    var jwkJson = JsonSerializer.Serialize(jwk);
                    var fingerprint = KeyFingerprint(key.Key);

                    await using var keyCmd = new NpgsqlCommand("""
                        INSERT INTO auth_signing_keys (key_id, client_id, status, jwk, fingerprint, approved_at)
                        VALUES (@key_id, @client_id, 'approved', @jwk, @fingerprint, NOW())
                        ON CONFLICT (key_id) DO NOTHING;
                    """, connection, tx);
                    keyCmd.Parameters.AddWithValue("key_id", key.KeyId);
                    keyCmd.Parameters.AddWithValue("client_id", clientId);
                    keyCmd.Parameters.AddWithValue("jwk", jwkJson);
                    keyCmd.Parameters.AddWithValue("fingerprint", (object?)fingerprint ?? DBNull.Value);
                    await keyCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            if (!string.IsNullOrEmpty(initialAdministratorThumbprint))
            {
                await using var adminCmd = new NpgsqlCommand("""
                    INSERT INTO auth_administrators (thumbprint)
                    VALUES (@t)
                    ON CONFLICT (thumbprint) DO NOTHING;
                """, connection, tx);
                adminCmd.Parameters.AddWithValue("t", initialAdministratorThumbprint);
                await adminCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            if (onInitializing is not null)
            {
                await onInitializing(tx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
        }

        // 2. 從資料庫載入所有狀態到記憶體快取與 TrustRegistry
        // (a) Clients enabled
        await using (var cmd = new NpgsqlCommand("SELECT client_id, enabled FROM auth_clients;", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var clientId = reader.GetString(0);
                var enabled = reader.GetBoolean(1);
                if (!enabled)
                {
                    registry.DisableClient(clientId);
                }
            }
        }

        // (b) Certificates
        await using (var cmd = new NpgsqlCommand("SELECT thumbprint, client_id, status, raw_cert FROM auth_certificates;", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var thumbprint = reader.GetString(0);
                var clientId = reader.GetString(1);
                var status = reader.GetString(2);
                var raw = reader.GetString(3);

                var cert = X509Certificate2.CreateFromPem(raw);
                if (!trustedCertificates.TryGetValue(clientId, out var list))
                {
                    list = new List<X509Certificate2>();
                    trustedCertificates[clientId] = list;
                }

                if (!list.Any(c => string.Equals(c.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(cert);
                }

                if (status == "retired")
                {
                    registry.RetireCertificate(thumbprint);
                }
                else if (status == "revoked")
                {
                    registry.RevokeCertificate(thumbprint);
                }
            }
        }

        // (c) Signing Keys
        await using (var cmd = new NpgsqlCommand("SELECT key_id, client_id, status, jwk FROM auth_signing_keys;", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var keyId = reader.GetString(0);
                var clientId = reader.GetString(1);
                var status = reader.GetString(2);
                var jwkJson = reader.GetString(3);

                var jwk = JsonWebKey.Create(jwkJson);
                var ecdsa = ECDsa.Create(new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    Q = new ECPoint
                    {
                        X = Base64UrlEncoder.DecodeBytes(jwk.X),
                        Y = Base64UrlEncoder.DecodeBytes(jwk.Y),
                    }
                });
                verificationKeys.Register(clientId, new SignatureKey(keyId, ecdsa));

                if (status == "retired")
                {
                    registry.RetireSigningKey(keyId);
                }
                else if (status == "revoked")
                {
                    registry.RevokeSigningKey(keyId);
                }
            }
        }

        // (d) Administrators
        await using (var cmd = new NpgsqlCommand("SELECT thumbprint FROM auth_administrators;", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var thumbprint = reader.GetString(0);
                registry.RegisterAdministratorCertificate(thumbprint);
            }
        }
    }

    private static async Task<RequestSubmissionOutcome?> TryReserveNonceAndCheckLimitAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        string clientId,
        string proofNonce,
        DateTimeOffset proofTimestamp,
        CancellationToken cancellationToken)
    {
        // 1. 持有證明 Nonce 登記去重（AC-39）：以 (client_id, nonce) PRIMARY KEY 防止重放
        try
        {
            await using var nonceCmd = new NpgsqlCommand("""
                INSERT INTO auth_pop_nonces (client_id, nonce, created_at, retain_until)
                VALUES (@client_id, @nonce, @created_at, @retain_until);
            """, connection, tx);
            nonceCmd.Parameters.AddWithValue("client_id", clientId);
            nonceCmd.Parameters.AddWithValue("nonce", proofNonce);
            nonceCmd.Parameters.AddWithValue("created_at", proofTimestamp);
            nonceCmd.Parameters.AddWithValue("retain_until", proofTimestamp.AddMinutes(5));
            await nonceCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            await tx.RollbackAsync(cancellationToken);
            return RequestSubmissionOutcome.DuplicateNonce;
        }

        // 2. 待核准上限檢查（AC-38）：以 FOR UPDATE 鎖定 Client 列
        await using (var lockCmd = new NpgsqlCommand(
            "SELECT client_id FROM auth_clients WHERE client_id = @client_id FOR UPDATE;",
            connection, tx))
        {
            lockCmd.Parameters.AddWithValue("client_id", clientId);
            await lockCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var countCmd = new NpgsqlCommand("""
            SELECT COUNT(*) FROM auth_registration_requests
            WHERE client_id = @client_id AND status = 'pending' AND expires_at > NOW();
        """, connection, tx))
        {
            countCmd.Parameters.AddWithValue("client_id", clientId);
            var count = (long)(await countCmd.ExecuteScalarAsync(cancellationToken))!;
            if (count >= PendingRequestLimitPerClient)
            {
                await tx.RollbackAsync(cancellationToken);
                return RequestSubmissionOutcome.LimitReached;
            }
        }

        return null;
    }

    public async Task<RequestSubmissionOutcome> SubmitCertificateRequestAsync(
        string clientId,
        string requestId,
        string pem,
        string thumbprint,
        string applicantThumbprint,
        string proofNonce,
        DateTimeOffset proofTimestamp,
        DateTimeOffset expiresAt,
        AdministrativeAuditRecord auditRecord,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (await TryReserveNonceAndCheckLimitAsync(connection, tx, clientId, proofNonce, proofTimestamp, cancellationToken) is { } limitOutcome)
            {
                return limitOutcome;
            }

            // 3. 建立申請
            await using (var insertCmd = new NpgsqlCommand("""
                INSERT INTO auth_registration_requests (
                    request_id, client_id, type, status, payload, fingerprint,
                    applicant_thumbprint, expires_at, submitted_at
                ) VALUES (
                    @request_id, @client_id, 'certificate', 'pending', @payload, @fingerprint,
                    @applicant_thumbprint, @expires_at, NOW()
                );
            """, connection, tx))
            {
                insertCmd.Parameters.AddWithValue("request_id", requestId);
                insertCmd.Parameters.AddWithValue("client_id", clientId);
                insertCmd.Parameters.AddWithValue("payload", pem);
                insertCmd.Parameters.AddWithValue("fingerprint", thumbprint);
                insertCmd.Parameters.AddWithValue("applicant_thumbprint", applicantThumbprint);
                insertCmd.Parameters.AddWithValue("expires_at", expiresAt);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 4. 同一交易內寫入 accepted 稽核
            await WriteAuditInternalAsync(connection, tx, auditRecord, cancellationToken);

            await tx.CommitAsync(cancellationToken);
            return RequestSubmissionOutcome.Accepted;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await SafeRollbackAsync(tx, cancellationToken);
            throw;
        }
    }

    private async Task<DecisionOutcome> DecideRequestInternalAsync(
        string requestId,
        string expectedType,
        bool approve,
        AdministrativeAuditRecord auditRecord,
        Func<NpgsqlConnection, NpgsqlTransaction, string, string, string?, Task<DecisionOutcome?>>? onApprovingAsync,
        CancellationToken cancellationToken)
    {
        NpgsqlConnection? connection = null;
        NpgsqlTransaction? tx = null;
        string? resolvedClientId = null;
        string? resolvedFingerprint = null;
        string? resolvedKeyId = null;

        try
        {
            connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            tx = await connection.BeginTransactionAsync(cancellationToken);

            string status;
            string clientId;
            string payload;
            string fingerprint;
            string? keyId;
            DateTimeOffset expiresAt;

            await using (var selectCmd = new NpgsqlCommand("""
                SELECT status, client_id, payload, fingerprint, expires_at, key_id
                FROM auth_registration_requests
                WHERE request_id = @id AND type = @type FOR UPDATE;
            """, connection, tx))
            {
                selectCmd.Parameters.AddWithValue("id", requestId);
                selectCmd.Parameters.AddWithValue("type", expectedType);
                await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await tx.RollbackAsync(cancellationToken);
                    return DecisionOutcome.NotFound;
                }

                status = reader.GetString(0);
                clientId = reader.GetString(1);
                payload = reader.GetString(2);
                fingerprint = reader.GetString(3);
                expiresAt = reader.GetDateTime(4);
                keyId = reader.IsDBNull(5) ? null : reader.GetString(5);

                resolvedClientId = clientId;
                resolvedFingerprint = fingerprint;
                resolvedKeyId = keyId;
            }

            if (status != "pending" || expiresAt <= DateTimeOffset.UtcNow)
            {
                await tx.RollbackAsync(cancellationToken);
                return DecisionOutcome.NotPending;
            }

            if (approve)
            {
                if (onApprovingAsync is not null)
                {
                    var preCheckOutcome = await onApprovingAsync(connection, tx, clientId, payload, fingerprint);
                    if (preCheckOutcome is not null)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return preCheckOutcome.Value;
                    }
                }

                await using (var updateCmd = new NpgsqlCommand("""
                    UPDATE auth_registration_requests
                    SET status = 'approved', decided_at = NOW()
                    WHERE request_id = @id;
                """, connection, tx))
                {
                    updateCmd.Parameters.AddWithValue("id", requestId);
                    await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                var finalAudit = auditRecord with { ClientId = clientId, Fingerprint = fingerprint, KeyId = resolvedKeyId };
                await WriteAuditInternalAsync(connection, tx, finalAudit, cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return DecisionOutcome.Approved;
            }
            else
            {
                await using (var updateCmd = new NpgsqlCommand("""
                    UPDATE auth_registration_requests
                    SET status = 'rejected', decided_at = NOW()
                    WHERE request_id = @id;
                """, connection, tx))
                {
                    updateCmd.Parameters.AddWithValue("id", requestId);
                    await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                var finalAudit = auditRecord with { ClientId = clientId, Fingerprint = fingerprint, KeyId = resolvedKeyId };
                await WriteAuditInternalAsync(connection, tx, finalAudit, cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return DecisionOutcome.Rejected;
            }
        }
        catch (AdministrativeAuditWriteFailedException)
        {
            await SafeRollbackAsync(tx, cancellationToken);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await SafeRollbackAsync(tx, cancellationToken);
            var failedAudit = auditRecord with { ClientId = resolvedClientId, Fingerprint = resolvedFingerprint, KeyId = resolvedKeyId, Outcome = "failed", Reason = ex.Message };
            await WriteStandaloneFailedAuditAsync(failedAudit, cancellationToken);
            throw;
        }
        finally
        {
            if (tx is not null)
            {
                await tx.DisposeAsync();
            }
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }
        }
    }

    public Task<DecisionOutcome> DecideCertificateRequestAsync(
        string requestId,
        bool approve,
        AdministrativeAuditRecord auditRecord,
        Func<string, X509Certificate2, NpgsqlTransaction, CancellationToken, Task>? onApproved = null,
        CancellationToken cancellationToken = default)
    {
        return DecideRequestInternalAsync(
            requestId,
            "certificate",
            approve,
            auditRecord,
            async (conn, tx, clientId, pem, thumbprint) =>
            {
                await using (var chkCmd = new NpgsqlCommand("SELECT 1 FROM auth_certificates WHERE thumbprint = @thumbprint;", conn, tx))
                {
                    chkCmd.Parameters.AddWithValue("thumbprint", (object?)thumbprint ?? DBNull.Value);
                    if (await chkCmd.ExecuteScalarAsync(cancellationToken) is not null)
                    {
                        return DecisionOutcome.AlreadyRegistered;
                    }
                }

                await using (var certCmd = new NpgsqlCommand("""
                    INSERT INTO auth_certificates (thumbprint, client_id, status, raw_cert, approved_at)
                    VALUES (@thumbprint, @client_id, 'approved', @raw_cert, NOW());
                """, conn, tx))
                {
                    certCmd.Parameters.AddWithValue("thumbprint", (object?)thumbprint ?? DBNull.Value);
                    certCmd.Parameters.AddWithValue("client_id", clientId);
                    certCmd.Parameters.AddWithValue("raw_cert", pem);
                    await certCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                if (onApproved is not null)
                {
                    var cert = X509Certificate2.CreateFromPem(pem);
                    await onApproved(clientId, cert, tx, cancellationToken);
                }

                return null;
            },
            cancellationToken);
    }

    public async Task<List<AdministrativeAuditRecord>> SnapshotAuditsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var list = new List<AdministrativeAuditRecord>();
        await using var cmd = new NpgsqlCommand("""
            SELECT correlation_id, occurred_at, operation, actor_status, actor_thumbprint,
                   client_id, subject, key_id, fingerprint, outcome, reason, result_status,
                   applicant_authenticated, applicant_client_id
            FROM auth_administrative_audits ORDER BY id ASC;
        """, connection);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AdministrativeAuditRecord(
                CorrelationId: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                OccurredAt: reader.GetDateTime(1),
                Operation: reader.GetString(2),
                ActorStatus: reader.GetString(3),
                ActorThumbprint: reader.IsDBNull(4) ? null : reader.GetString(4),
                ClientId: reader.IsDBNull(5) ? null : reader.GetString(5),
                Subject: reader.IsDBNull(6) ? null : reader.GetString(6),
                KeyId: reader.IsDBNull(7) ? null : reader.GetString(7),
                Fingerprint: reader.IsDBNull(8) ? null : reader.GetString(8),
                Outcome: reader.GetString(9),
                Reason: reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                ResultStatus: reader.GetInt32(11),
                ApplicantAuthenticated: reader.IsDBNull(12) ? null : reader.GetBoolean(12),
                ApplicantClientId: reader.IsDBNull(13) ? null : reader.GetString(13)
            ));
        }

        return list;
    }

    public static async Task WriteAuditInternalAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, AdministrativeAuditRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO auth_administrative_audits (
                    correlation_id, occurred_at, operation, actor_status, actor_thumbprint,
                    client_id, subject, key_id, fingerprint, outcome, reason, result_status,
                    applicant_authenticated, applicant_client_id
                ) VALUES (
                    @correlation_id, @occurred_at, @operation, @actor_status, @actor_thumbprint,
                    @client_id, @subject, @key_id, @fingerprint, @outcome, @reason, @result_status,
                    @applicant_authenticated, @applicant_client_id
                );
            """, connection, transaction);

            cmd.Parameters.AddWithValue("correlation_id", (object?)record.CorrelationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("occurred_at", record.OccurredAt);
            cmd.Parameters.AddWithValue("operation", record.Operation);
            cmd.Parameters.AddWithValue("actor_status", record.ActorStatus);
            cmd.Parameters.AddWithValue("actor_thumbprint", (object?)record.ActorThumbprint ?? DBNull.Value);
            cmd.Parameters.AddWithValue("client_id", (object?)record.ClientId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("subject", (object?)record.Subject ?? DBNull.Value);
            cmd.Parameters.AddWithValue("key_id", (object?)record.KeyId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fingerprint", (object?)record.Fingerprint ?? DBNull.Value);
            cmd.Parameters.AddWithValue("outcome", record.Outcome);
            cmd.Parameters.AddWithValue("reason", (object?)record.Reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("result_status", record.ResultStatus);
            cmd.Parameters.AddWithValue("applicant_authenticated", (object?)record.ApplicantAuthenticated ?? DBNull.Value);
            cmd.Parameters.AddWithValue("applicant_client_id", (object?)record.ApplicantClientId ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not AdministrativeAuditWriteFailedException && ex is not OperationCanceledException)
        {
            throw new AdministrativeAuditWriteFailedException($"寫入稽核紀錄失敗：{ex.Message}", ex);
        }
    }

    public async Task WriteStandaloneFailedAuditAsync(AdministrativeAuditRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await WriteAuditInternalAsync(connection, null, record with { Outcome = "failed" }, cancellationToken);
        }
        catch (Exception ex) when (ex is not AdministrativeAuditWriteFailedException && ex is not OperationCanceledException)
        {
            throw new AdministrativeAuditWriteFailedException("寫入 failed 稽核紀錄失敗。", ex);
        }
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        return conn;
    }

    internal static async Task ApplyAdministrativeStateChangeAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        string operation,
        AdministrativeTarget target,
        CancellationToken cancellationToken = default)
    {
        if (operation == "client.disable" && target.ClientId is { } cid)
        {
            await using var cmd = new NpgsqlCommand("UPDATE auth_clients SET enabled = false WHERE client_id = @id;", conn, tx);
            cmd.Parameters.AddWithValue("id", cid);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (operation == "certificate.retire" && target.Fingerprint is { } rFp)
        {
            await using var cmd = new NpgsqlCommand("UPDATE auth_certificates SET status = 'retired', retired_at = NOW() WHERE thumbprint = @thumbprint;", conn, tx);
            cmd.Parameters.AddWithValue("thumbprint", rFp);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (operation == "certificate.revoke" && target.Fingerprint is { } vFp)
        {
            await using var cmd = new NpgsqlCommand("UPDATE auth_certificates SET status = 'revoked', revoked_at = NOW() WHERE thumbprint = @thumbprint;", conn, tx);
            cmd.Parameters.AddWithValue("thumbprint", vFp);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (operation == "signing_key.retire" && target.KeyId is { } rKid)
        {
            await using var cmd = new NpgsqlCommand("UPDATE auth_signing_keys SET status = 'retired', retired_at = NOW() WHERE key_id = @keyId;", conn, tx);
            cmd.Parameters.AddWithValue("keyId", rKid);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (operation == "signing_key.revoke" && target.KeyId is { } vKid)
        {
            await using var cmd = new NpgsqlCommand("UPDATE auth_signing_keys SET status = 'revoked', revoked_at = NOW() WHERE key_id = @keyId;", conn, tx);
            cmd.Parameters.AddWithValue("keyId", vKid);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task DisableClientAsync(string clientId, AdministrativeAuditRecord auditRecord, CancellationToken cancellationToken = default) =>
        await ExecuteTransactionalAdminOperationAsync(auditRecord, (conn, tx, ct) =>
            ApplyAdministrativeStateChangeAsync(conn, tx, "client.disable", new AdministrativeTarget(clientId), ct), cancellationToken);

    public async Task RetireCertificateAsync(string thumbprint, AdministrativeAuditRecord auditRecord, CancellationToken cancellationToken = default) =>
        await ExecuteTransactionalAdminOperationAsync(auditRecord, (conn, tx, ct) =>
            ApplyAdministrativeStateChangeAsync(conn, tx, "certificate.retire", new AdministrativeTarget(null, Fingerprint: thumbprint), ct), cancellationToken);

    public async Task RevokeCertificateAsync(string thumbprint, AdministrativeAuditRecord auditRecord, CancellationToken cancellationToken = default) =>
        await ExecuteTransactionalAdminOperationAsync(auditRecord, (conn, tx, ct) =>
            ApplyAdministrativeStateChangeAsync(conn, tx, "certificate.revoke", new AdministrativeTarget(null, Fingerprint: thumbprint), ct), cancellationToken);

    public async Task RetireSigningKeyAsync(string keyId, AdministrativeAuditRecord auditRecord, CancellationToken cancellationToken = default) =>
        await ExecuteTransactionalAdminOperationAsync(auditRecord, (conn, tx, ct) =>
            ApplyAdministrativeStateChangeAsync(conn, tx, "signing_key.retire", new AdministrativeTarget(null, KeyId: keyId), ct), cancellationToken);

    public async Task RevokeSigningKeyAsync(string keyId, AdministrativeAuditRecord auditRecord, CancellationToken cancellationToken = default) =>
        await ExecuteTransactionalAdminOperationAsync(auditRecord, (conn, tx, ct) =>
            ApplyAdministrativeStateChangeAsync(conn, tx, "signing_key.revoke", new AdministrativeTarget(null, KeyId: keyId), ct), cancellationToken);

    private async Task ExecuteTransactionalAdminOperationAsync(
        AdministrativeAuditRecord auditRecord,
        Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(connection, tx, cancellationToken);
            await WriteAuditInternalAsync(connection, tx, auditRecord, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (AdministrativeAuditWriteFailedException)
        {
            await SafeRollbackAsync(tx, cancellationToken);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await SafeRollbackAsync(tx, cancellationToken);
            await WriteStandaloneFailedAuditAsync(auditRecord with { Outcome = "failed", Reason = ex.Message }, cancellationToken);
            throw;
        }
    }

    public async Task<RequestSubmissionOutcome> SubmitSigningKeyRequestAsync(
        string clientId,
        string requestId,
        string jwkJson,
        string keyId,
        string fingerprint,
        string applicantThumbprint,
        string proofNonce,
        DateTimeOffset proofTimestamp,
        DateTimeOffset expiresAt,
        AdministrativeAuditRecord auditRecord,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (await TryReserveNonceAndCheckLimitAsync(connection, tx, clientId, proofNonce, proofTimestamp, cancellationToken) is { } limitOutcome)
            {
                return limitOutcome;
            }

            await using (var insertCmd = new NpgsqlCommand("""
                INSERT INTO auth_registration_requests (
                    request_id, client_id, type, key_id, status, payload, fingerprint,
                    applicant_thumbprint, expires_at, submitted_at
                ) VALUES (
                    @request_id, @client_id, 'signing_key', @key_id, 'pending', @payload, @fingerprint,
                    @applicant_thumbprint, @expires_at, NOW()
                );
            """, connection, tx))
            {
                insertCmd.Parameters.AddWithValue("request_id", requestId);
                insertCmd.Parameters.AddWithValue("client_id", clientId);
                insertCmd.Parameters.AddWithValue("key_id", keyId);
                insertCmd.Parameters.AddWithValue("payload", jwkJson);
                insertCmd.Parameters.AddWithValue("fingerprint", fingerprint);
                insertCmd.Parameters.AddWithValue("applicant_thumbprint", applicantThumbprint);
                insertCmd.Parameters.AddWithValue("expires_at", expiresAt);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await WriteAuditInternalAsync(connection, tx, auditRecord, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return RequestSubmissionOutcome.Accepted;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await SafeRollbackAsync(tx, cancellationToken);
            throw;
        }
    }

    public Task<DecisionOutcome> DecideSigningKeyRequestAsync(
        string requestId,
        bool approve,
        AdministrativeAuditRecord auditRecord,
        Func<string, string, string, CancellationToken, Task>? onApproved = null,
        CancellationToken cancellationToken = default)
    {
        return DecideRequestInternalAsync(
            requestId,
            "signing_key",
            approve,
            auditRecord,
            async (conn, tx, clientId, jwkJson, fingerprint) =>
            {
                var jwk = JsonWebKey.Create(jwkJson);
                var keyId = jwk.Kid;

                await using (var chkCmd = new NpgsqlCommand("SELECT 1 FROM auth_signing_keys WHERE key_id = @keyId;", conn, tx))
                {
                    chkCmd.Parameters.AddWithValue("keyId", keyId);
                    if (await chkCmd.ExecuteScalarAsync(cancellationToken) is not null)
                    {
                        return DecisionOutcome.AlreadyRegistered;
                    }
                }

                await using (var keyCmd = new NpgsqlCommand("""
                    INSERT INTO auth_signing_keys (key_id, client_id, status, jwk, fingerprint, approved_at)
                    VALUES (@keyId, @client_id, 'approved', @jwk, @fingerprint, NOW());
                """, conn, tx))
                {
                    keyCmd.Parameters.AddWithValue("keyId", keyId);
                    keyCmd.Parameters.AddWithValue("client_id", clientId);
                    keyCmd.Parameters.AddWithValue("jwk", jwkJson);
                    keyCmd.Parameters.AddWithValue("fingerprint", (object?)fingerprint ?? DBNull.Value);
                    await keyCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                if (onApproved is not null)
                {
                    await onApproved(clientId, keyId, jwkJson, cancellationToken);
                }

                return null;
            },
            cancellationToken);
    }

    public async Task<RegistrationRequestRecord?> GetCertificateRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("""
            SELECT request_id, client_id, fingerprint, status, applicant_thumbprint, expires_at
            FROM auth_registration_requests WHERE request_id = @id AND type = 'certificate';
        """, connection);
        cmd.Parameters.AddWithValue("id", requestId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new RegistrationRequestRecord(
            RequestId: reader.GetString(0),
            ClientId: reader.GetString(1),
            Fingerprint: reader.GetString(2),
            Status: reader.GetString(3),
            ApplicantThumbprint: reader.GetString(4),
            ExpiresAt: reader.GetDateTime(5),
            KeyId: null);
    }

    public async Task<RegistrationRequestRecord?> GetSigningKeyRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("""
            SELECT request_id, client_id, key_id, fingerprint, status, applicant_thumbprint, expires_at
            FROM auth_registration_requests WHERE request_id = @id AND type = 'signing_key';
        """, connection);
        cmd.Parameters.AddWithValue("id", requestId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new RegistrationRequestRecord(
            RequestId: reader.GetString(0),
            ClientId: reader.GetString(1),
            Fingerprint: reader.GetString(3),
            Status: reader.GetString(4),
            ApplicantThumbprint: reader.GetString(5),
            ExpiresAt: reader.GetDateTime(6),
            KeyId: reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    public async Task<bool> ClientExistsAsync(string clientId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM auth_clients WHERE client_id = @id;", connection);
        cmd.Parameters.AddWithValue("id", clientId);
        return await cmd.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<bool> IsClientEnabledAsync(string clientId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT enabled FROM auth_clients WHERE client_id = @id;", connection);
        cmd.Parameters.AddWithValue("id", clientId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is bool b && b;
    }

    public async Task<bool> IsCertificateBlockedAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT status FROM auth_certificates WHERE thumbprint = @thumbprint;", connection);
        cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is string s && (s == "retired" || s == "revoked");
    }

    public async Task<bool> IsAdministratorCertificateAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM auth_administrators WHERE thumbprint = @thumbprint;", connection);
        cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        return await cmd.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<string?> FindClientByCertificateAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT client_id FROM auth_certificates WHERE thumbprint = @thumbprint AND status = 'approved';", connection);
        cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        return (string?)await cmd.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<string?> GetCertificateStatusAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT status FROM auth_certificates WHERE thumbprint = @thumbprint;", connection);
        cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        return (string?)await cmd.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<string?> GetCertificateClientIdAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT client_id FROM auth_certificates WHERE thumbprint = @thumbprint;", connection);
        cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        return (string?)await cmd.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<bool> IsCertificateSharedWithOtherClientAsync(string clientId, string thumbprint, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM auth_certificates WHERE thumbprint = @thumbprint AND client_id <> @clientId;", connection);
        cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        cmd.Parameters.AddWithValue("clientId", clientId);
        return await cmd.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<bool> HasActiveAlternativeCertificateAsync(string clientId, string currentThumbprint, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("""
            SELECT 1 FROM auth_certificates 
            WHERE client_id = @clientId AND thumbprint <> @thumbprint AND status = 'approved';
        """, connection);
        cmd.Parameters.AddWithValue("clientId", clientId);
        cmd.Parameters.AddWithValue("thumbprint", currentThumbprint);
        return await cmd.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<string?> GetSigningKeyStatusAsync(string keyId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT status FROM auth_signing_keys WHERE key_id = @keyId;", connection);
        cmd.Parameters.AddWithValue("keyId", keyId);
        return (string?)await cmd.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<string?> GetSigningKeyClientIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT client_id FROM auth_signing_keys WHERE key_id = @keyId;", connection);
        cmd.Parameters.AddWithValue("keyId", keyId);
        return (string?)await cmd.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<bool> IsSigningKeySharedWithOtherClientAsync(string clientId, string keyId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM auth_signing_keys WHERE key_id = @keyId AND client_id <> @clientId;", connection);
        cmd.Parameters.AddWithValue("keyId", keyId);
        cmd.Parameters.AddWithValue("clientId", clientId);
        return await cmd.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<string?> GetSigningKeyFingerprintAsync(string keyId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT fingerprint FROM auth_signing_keys WHERE key_id = @keyId;", connection);
        cmd.Parameters.AddWithValue("keyId", keyId);
        return (string?)await cmd.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<bool> HasActiveAlternativeSigningKeyAsync(string clientId, string currentKeyId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("""
            SELECT 1 FROM auth_signing_keys 
            WHERE client_id = @clientId AND key_id <> @keyId AND status = 'approved';
        """, connection);
        cmd.Parameters.AddWithValue("clientId", clientId);
        cmd.Parameters.AddWithValue("keyId", currentKeyId);
        return await cmd.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<object[]> GetTrustListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var clients = new Dictionary<string, (bool Enabled, List<string> ApprovedThumbprints, List<object> Certificates, List<string> ApprovedSigningKeys, List<object> SigningKeys)>();

        await using (var cmd = new NpgsqlCommand("SELECT client_id, enabled FROM auth_clients ORDER BY client_id;", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                clients[reader.GetString(0)] = (reader.GetBoolean(1), [], [], [], []);
            }
        }

        await using (var cmd = new NpgsqlCommand("SELECT client_id, thumbprint, status FROM auth_certificates ORDER BY thumbprint;", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var cid = reader.GetString(0);
                var thumbprint = reader.GetString(1);
                var status = reader.GetString(2);
                if (clients.TryGetValue(cid, out var entry))
                {
                    if (status == "approved")
                    {
                        entry.ApprovedThumbprints.Add(thumbprint);
                    }
                    entry.Certificates.Add(new { thumbprint, status });
                }
            }
        }

        await using (var cmd = new NpgsqlCommand("SELECT client_id, key_id, status FROM auth_signing_keys ORDER BY key_id;", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var cid = reader.GetString(0);
                var keyId = reader.GetString(1);
                var status = reader.GetString(2);
                if (clients.TryGetValue(cid, out var entry))
                {
                    if (status == "approved")
                    {
                        entry.ApprovedSigningKeys.Add(keyId);
                    }
                    entry.SigningKeys.Add(new { keyId, status });
                }
            }
        }

        return clients
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => (object)new
            {
                clientId = p.Key,
                enabled = p.Value.Enabled,
                certificateThumbprints = p.Value.ApprovedThumbprints.ToArray(),
                certificates = p.Value.Certificates.ToArray(),
                signingKeyIds = p.Value.ApprovedSigningKeys.ToArray(),
                signingKeys = p.Value.SigningKeys.ToArray(),
            })
            .ToArray();
    }

    internal static string KeyFingerprint(ECDsa key)
    {
        var parameters = key.ExportParameters(false);
        var x = parameters.Q.X ?? throw new InvalidOperationException("EC key X is missing.");
        var y = parameters.Q.Y ?? throw new InvalidOperationException("EC key Y is missing.");
        var bytes = new byte[x.Length + y.Length];
        Buffer.BlockCopy(x, 0, bytes, 0, x.Length);
        Buffer.BlockCopy(y, 0, bytes, x.Length, y.Length);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static async Task SafeRollbackAsync(NpgsqlTransaction? tx, CancellationToken callerCt = default)
    {
        if (tx is null) return;
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(callerCt, timeoutCts.Token);
        try
        {
            await tx.RollbackAsync(linkedCts.Token);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !callerCt.IsCancellationRequested)
        {
            System.Diagnostics.Trace.TraceWarning("[PostgresAuthStore] SafeRollbackAsync timed out: {0}", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Trace.TraceWarning("[PostgresAuthStore] SafeRollbackAsync failed: {0}", ex.Message);
        }
    }

    /// <summary>測試用：重設資料表內容至乾淨狀態，受控封裝 TRUNCATE 語法避免測試直接操作業務表。</summary>
    public async Task ResetForTestingAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("""
            GRANT ALL ON ALL TABLES IN SCHEMA public TO auth_svc;
            TRUNCATE TABLE auth_administrative_audits, auth_pop_nonces, auth_registration_requests, auth_certificates, auth_signing_keys, auth_administrators, auth_clients CASCADE;
            DO $$
            BEGIN
                IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'OpenIddictTokens') THEN
                    TRUNCATE TABLE "OpenIddictTokens", "OpenIddictAuthorizations", "OpenIddictApplications", "OpenIddictScopes" CASCADE;
                END IF;
            END $$;
        """, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>測試用：在憑證表或簽章金鑰表注入寫入故障約束。</summary>
    public async Task SetSimulatedFailureConstraintAsync(bool failAuditAlso = false, string table = "both", CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var tableSql = table switch
        {
            "auth_certificates" => "ALTER TABLE auth_certificates ADD CONSTRAINT fail_all_insert CHECK (false) NOT VALID;",
            "auth_signing_keys" => "ALTER TABLE auth_signing_keys ADD CONSTRAINT fail_all_insert_key CHECK (false) NOT VALID;",
            _ => """
                ALTER TABLE auth_certificates ADD CONSTRAINT fail_all_insert CHECK (false) NOT VALID;
                ALTER TABLE auth_signing_keys ADD CONSTRAINT fail_all_insert_key CHECK (false) NOT VALID;
            """,
        };

        var sql = failAuditAlso
            ? $"{tableSql}\nALTER TABLE auth_administrative_audits ADD CONSTRAINT fail_all_audit CHECK (false) NOT VALID;"
            : tableSql;

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>測試用：清除注入的故障約束。</summary>
    public async Task ClearSimulatedFailureConstraintsAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("""
            ALTER TABLE auth_certificates DROP CONSTRAINT IF EXISTS fail_all_insert;
            ALTER TABLE auth_signing_keys DROP CONSTRAINT IF EXISTS fail_all_insert_key;
            ALTER TABLE auth_administrative_audits DROP CONSTRAINT IF EXISTS fail_all_audit;
            GRANT ALL ON ALL TABLES IN SCHEMA public TO auth_svc;
        """, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
