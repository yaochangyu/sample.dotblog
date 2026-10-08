using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Lab.Creds.Proof.Signatures;

// A deliberately small, strict subset of RFC 8941 Structured Fields: just what the Lab profile and RFC 9530 digests need.
// Only the canonical serialization is accepted (single SP between list items, no optional whitespace, no repeated or
// bare parameters, no comma-separated members), so "accepted" always means "unambiguous". Everything else is rejected.
public static class StructuredFieldParser
{
    private const int MaxLength = 4096;

    // sig1=("a" "b");name=1;name="text"   (exactly one dictionary member whose value is an inner list)
    public static bool TryParseSignatureInput(string value, [NotNullWhen(true)] out ParsedSignatureInput? parsed)
    {
        parsed = null;
        if (value.Length is 0 or > MaxLength) return false;

        var i = 0;
        if (!ReadKey(value, ref i, out var label) || !Eat(value, ref i, '=')) return false;
        var valueStart = i;
        if (!Eat(value, ref i, '(')) return false;

        var components = new List<string>();
        if (!Eat(value, ref i, ')'))
        {
            while (true)
            {
                if (!ReadString(value, ref i, out var component)) return false;
                components.Add(component);
                if (Eat(value, ref i, ')')) break;
                if (!Eat(value, ref i, ' ')) return false;
            }
        }

        var parameters = new List<SignatureParameter>();
        while (Eat(value, ref i, ';'))
        {
            if (!ReadKey(value, ref i, out var name) || !Eat(value, ref i, '=')) return false;
            if (parameters.Exists(p => p.Name == name)) return false;

            if (i < value.Length && value[i] == '"')
            {
                if (!ReadString(value, ref i, out var text)) return false;
                parameters.Add(new SignatureParameter(name, text));
            }
            else
            {
                if (!ReadInteger(value, ref i, out var number)) return false;
                parameters.Add(new SignatureParameter(name, number));
            }
        }

        if (i != value.Length) return false;
        parsed = new ParsedSignatureInput(label, components, parameters, value[valueStart..]);
        return true;
    }

    // sig1=:<standard base64>:   (exactly one member)
    public static bool TryParseSignature(string value, [NotNullWhen(true)] out string? label, [NotNullWhen(true)] out byte[]? signature)
    {
        label = null;
        signature = null;
        if (value.Length is 0 or > MaxLength) return false;

        var i = 0;
        if (!ReadKey(value, ref i, out var key) || !Eat(value, ref i, '=') || !ReadByteSequence(value, ref i, out var bytes) || i != value.Length)
        {
            return false;
        }

        label = key;
        signature = bytes;
        return true;
    }

    // sha-256=:<base64>:, sha-512=:<base64>:   (RFC 9530 Content-Digest: byte-sequence members only, unique keys)
    public static bool TryParseDigestDictionary(string value, [NotNullWhen(true)] out IReadOnlyDictionary<string, byte[]>? members)
    {
        members = null;
        if (value.Length is 0 or > MaxLength) return false;

        var result = new Dictionary<string, byte[]>();
        var i = 0;
        while (true)
        {
            if (!ReadKey(value, ref i, out var key) || !Eat(value, ref i, '=') || !ReadByteSequence(value, ref i, out var bytes)) return false;
            if (!result.TryAdd(key, bytes)) return false;
            if (i == value.Length) break;
            if (!Eat(value, ref i, ',')) return false;
            while (i < value.Length && value[i] is ' ' or '\t') i++;
            if (i == value.Length) return false;
        }

        members = result;
        return true;
    }

    private static bool Eat(string value, ref int i, char expected)
    {
        if (i >= value.Length || value[i] != expected) return false;
        i++;
        return true;
    }

    private static bool ReadKey(string value, ref int i, out string key)
    {
        var start = i;
        key = "";
        if (i >= value.Length || !(value[i] is >= 'a' and <= 'z' || value[i] == '*')) return false;
        while (i < value.Length && (value[i] is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '.' or '*')) i++;
        key = value[start..i];
        return true;
    }

    private static bool ReadString(string value, ref int i, out string text)
    {
        text = "";
        if (!Eat(value, ref i, '"')) return false;
        var builder = new StringBuilder();
        while (i < value.Length)
        {
            var c = value[i++];
            if (c == '"')
            {
                text = builder.ToString();
                return true;
            }

            if (c == '\\')
            {
                if (i >= value.Length || value[i] is not ('"' or '\\')) return false;
                c = value[i++];
            }
            else if (c is < ' ' or > '~')
            {
                return false;
            }

            builder.Append(c);
        }

        return false;
    }

    private static bool ReadInteger(string value, ref int i, out long number)
    {
        number = 0;
        var start = i;
        if (i < value.Length && value[i] == '-') i++;
        var digits = i;
        while (i < value.Length && value[i] is >= '0' and <= '9') i++;
        if (i == digits || i - digits > 15) return false;
        return long.TryParse(value.AsSpan(start, i - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);
    }

    private static bool ReadByteSequence(string value, ref int i, out byte[] bytes)
    {
        bytes = [];
        if (!Eat(value, ref i, ':')) return false;
        var start = i;
        while (i < value.Length && (value[i] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '/' or '=')) i++;
        var encoded = value.AsSpan(start, i - start);
        if (!Eat(value, ref i, ':') || encoded.Length == 0) return false;

        var buffer = new byte[encoded.Length];
        if (!Convert.TryFromBase64Chars(encoded, buffer, out var written)) return false;
        bytes = buffer[..written];
        return true;
    }
}
