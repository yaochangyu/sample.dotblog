namespace AuthSpike.Signing;

/// <summary>解析 Signature-Input 標頭與其中的時間欄位；只做語法解析，不做任何信任判斷。</summary>
internal static class SignatureInputParser
{
    /// <summary>解析 Signature-Input（sig1=(...);k=v;...），parameters 為 label 之後的原文，即簽署時的 @signature-params。</summary>
    public static bool TryParse(
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

    public static bool TryReadTime(IReadOnlyDictionary<string, string> fields, string name, out DateTimeOffset time)
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
