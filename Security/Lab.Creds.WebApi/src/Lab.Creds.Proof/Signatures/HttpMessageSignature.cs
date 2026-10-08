using System.Security.Cryptography;
using System.Text;

namespace Lab.Creds.Proof.Signatures;

// RFC 9421 §2.5 signature base and the ecdsa-p256-sha256 primitive (§3.3.4), using platform cryptography only.
public static class HttpMessageSignature
{
    public static string BuildSignatureBase(ParsedSignatureInput input, Func<string, string> componentValue)
    {
        var builder = new StringBuilder();
        foreach (var component in input.Components)
        {
            builder.Append('"').Append(component.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\": ").Append(componentValue(component)).Append('\n');
        }

        return builder.Append("\"@signature-params\": ").Append(input.SignatureParamsValue).ToString();
    }

    // IEEE P1363 r||s (64 bytes) is the default signature format of ECDsa.VerifyData; no DER conversion happens.
    public static bool VerifyEcdsaP256(byte[] subjectPublicKeyInfo, string signatureBase, byte[] signature)
    {
        using var key = ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
        if (key.KeySize != 256 || key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7") return false;
        return signature.Length == 64 && key.VerifyData(Encoding.ASCII.GetBytes(signatureBase), signature, HashAlgorithmName.SHA256);
    }
}
