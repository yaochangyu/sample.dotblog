using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using AuthSpike.Replay;
using AuthSpike.Trust;

namespace AuthSpike.Signing;

/// <summary>簽章驗證結果：只有 Accepted 才能進入業務處理。</summary>
public enum SignatureOutcome
{
    /// <summary>簽章、時間窗與防重放皆通過。</summary>
    Accepted,

    /// <summary>簽章、時間窗或必要欄位不成立。</summary>
    Rejected,

    /// <summary>簽章有效，但同一 (client_id, nonce) 已被接受過（重放）。</summary>
    Replayed,

    /// <summary>防重放狀態無法可靠讀寫；必須明確回報服務錯誤，不得放行。</summary>
    ReplayStateUnavailable,
}

/// <summary>
/// 業務 API 端：驗證原始呼叫端的 HTTP Message Signature，並以共用 nonce 儲存做跨執行個體防重放。
/// 只使用已驗證 Token 所識別的 Client 登錄之簽章金鑰；不採信請求自行宣稱的金鑰或身分。
/// 時間窗與保存期（lab 暫定，待使用者確認）：created 不超前超過 30 秒、有效期（expires - created）不超過 60 秒、
/// 未達 expires 才接受；nonce 保存至 expires 加 30 秒。
/// </summary>
public sealed class SignedRequestVerifier(IReadOnlyDictionary<string, SignatureKey> keysByClient, NonceReplayStore replayStore, TrustRegistry registry)
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxValidity = TimeSpan.FromSeconds(60);

    public async Task<SignatureOutcome> VerifyAsync(HttpContext context, string verifiedClientId)
    {
        var accepted = await ValidateSignatureAsync(context, verifiedClientId);
        if (accepted is null)
        {
            return SignatureOutcome.Rejected;
        }

        try
        {
            // 防重放在簽章與時間窗通過之後、業務副作用之前登錄，確保重放不會進入業務處理。
            var registered = replayStore.TryRegister(verifiedClientId, accepted.Nonce, accepted.Expires + ClockSkew);
            return registered ? SignatureOutcome.Accepted : SignatureOutcome.Replayed;
        }
        catch (ReplayStateUnavailableException)
        {
            return SignatureOutcome.ReplayStateUnavailable;
        }
    }

    private sealed record AcceptedSignature(string Nonce, DateTimeOffset Expires);

    private async Task<AcceptedSignature?> ValidateSignatureAsync(HttpContext context, string verifiedClientId)
    {
        if (!keysByClient.TryGetValue(verifiedClientId, out var signatureKey))
        {
            return null;
        }

        var headers = context.Request.Headers;
        var signatureInput = headers["Signature-Input"].ToString();
        var signatureHeader = headers["Signature"].ToString();
        if (!TryParseSignatureInput(signatureInput, out var components, out var parameters, out var fields))
        {
            return null;
        }

        var required = HttpMessageSignature.RequiredComponents(context.Request.Method);
        if (components.Count != required.Length || !required.All(components.Contains))
        {
            return null;
        }

        if (!fields.TryGetValue("alg", out var algorithm) || algorithm != HttpMessageSignature.Algorithm)
        {
            return null;
        }

        // keyid 必須是「已驗證 Client」登錄的金鑰，混用其他 Client 的合法金鑰一律拒絕。
        if (!fields.TryGetValue("keyid", out var keyId) || keyId != signatureKey.KeyId)
        {
            return null;
        }

        // 已撤銷的簽章金鑰即使屬於已驗證 Client 也不被接受。
        if (registry.IsSigningKeyRevoked(keyId))
        {
            return null;
        }

        if (!TryReadTime(fields, "created", out var created) || !TryReadTime(fields, "expires", out var expires)
            || !fields.TryGetValue("nonce", out var nonce) || string.IsNullOrEmpty(nonce))
        {
            return null;
        }

        // 有效期上限：保存期以 expires 為基準，有效期無上限時 nonce 紀錄也會無限期保存。
        if (expires - created > MaxValidity)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (created > now + ClockSkew || now >= expires)
        {
            return null;
        }

        context.Request.EnableBuffering();
        using var buffer = new MemoryStream();
        await context.Request.Body.CopyToAsync(buffer);
        context.Request.Body.Position = 0;
        var body = buffer.ToArray();

        var values = new Dictionary<string, string>
        {
            ["@method"] = context.Request.Method,
            ["@target-uri"] = context.Request.GetEncodedUrl(),
            ["@query"] = string.IsNullOrEmpty(context.Request.QueryString.Value) ? "?" : context.Request.QueryString.Value,
            ["authorization"] = headers["Authorization"].ToString(),
        };

        if (required.Contains("content-digest"))
        {
            var digest = headers["Content-Digest"].ToString();
            // 核對收到的實際 Body 與已簽署的摘要；只驗證摘要欄位的簽章不足以證明內容未被改動。
            if (digest != HttpMessageSignature.ContentDigest(body))
            {
                return null;
            }

            values["content-type"] = headers["Content-Type"].ToString();
            values["content-digest"] = digest;
            values["idempotency-key"] = headers["Idempotency-Key"].ToString();
        }

        if (required.Any(component => !values.TryGetValue(component, out var value) || value.Length == 0))
        {
            return null;
        }

        return VerifySignature(signatureKey, signatureHeader, components, values, parameters)
            ? new AcceptedSignature(nonce, expires)
            : null;
    }

    private static bool VerifySignature(
        SignatureKey signatureKey,
        string signatureHeader,
        IReadOnlyList<string> components,
        IReadOnlyDictionary<string, string> values,
        string parameters)
    {
        var prefix = $"{HttpMessageSignature.Label}=:";
        if (!signatureHeader.StartsWith(prefix, StringComparison.Ordinal)
            || signatureHeader.Length <= prefix.Length + 1
            || !signatureHeader.EndsWith(':'))
        {
            return false;
        }

        try
        {
            var signature = Convert.FromBase64String(signatureHeader[prefix.Length..^1]);
            var signatureBase = HttpMessageSignature.BuildSignatureBase(components, values, parameters);
            return signatureKey.Key.VerifyData(
                Encoding.UTF8.GetBytes(signatureBase),
                signature,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return false;
        }
    }

    /// <summary>解析 Signature-Input（sig1=(...);k=v;...），parameters 為 label 之後的原文，即簽署時的 @signature-params。</summary>
    private static bool TryParseSignatureInput(
        string header,
        out List<string> components,
        out string parameters,
        out Dictionary<string, string> fields)
    {
        components = [];
        parameters = string.Empty;
        fields = [];

        var prefix = $"{HttpMessageSignature.Label}=";
        if (!header.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        parameters = header[prefix.Length..];
        if (!parameters.StartsWith('('))
        {
            return false;
        }

        var close = parameters.IndexOf(')');
        if (close < 0)
        {
            return false;
        }

        foreach (var item in parameters[1..close].Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (item.Length < 3 || item[0] != '"' || item[^1] != '"')
            {
                return false;
            }

            components.Add(item[1..^1]);
        }

        foreach (var part in parameters[(close + 1)..].Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                return false;
            }

            fields[part[..separator]] = part[(separator + 1)..].Trim('"');
        }

        return true;
    }

    private static bool TryReadTime(IReadOnlyDictionary<string, string> fields, string name, out DateTimeOffset time)
    {
        time = default;
        return fields.TryGetValue(name, out var raw)
            && long.TryParse(raw, out var seconds)
            && TryFromUnix(seconds, out time);
    }

    private static bool TryFromUnix(long seconds, out DateTimeOffset time)
    {
        time = default;
        if (seconds < DateTimeOffset.MinValue.ToUnixTimeSeconds() || seconds > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
        {
            return false;
        }

        time = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }
}
