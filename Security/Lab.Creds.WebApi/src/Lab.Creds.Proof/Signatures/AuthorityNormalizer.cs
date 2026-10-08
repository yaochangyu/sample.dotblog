namespace Lab.Creds.Proof.Signatures;

// RFC 9421 2.2.3 / RFC 9110 4.2.3: the derived @authority is lower-case and omits the port only when it is the
// default of the request scheme (https 443, http 80). The Host header itself is never rewritten.
public static class AuthorityNormalizer
{
    public static string Normalize(string host, string scheme)
    {
        var defaultPort = scheme.ToLowerInvariant() switch
        {
            "https" => "443",
            "http" => "80",
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null)
        };
        var lower = host.ToLowerInvariant();
        var portSeparator = lower.LastIndexOf(':');
        var hasPort = portSeparator > lower.LastIndexOf(']') && portSeparator >= 0;
        if (!hasPort) return lower;
        return lower[(portSeparator + 1)..] == defaultPort ? lower[..portSeparator] : lower;
    }
}
