using System.Text;

namespace Lab.Creds.Proof.Tests.Support;

public static class SignedRequests
{
    public const string DemoBody = """{"partnerName":"Acme","payload":"demo"}""";

    public static TestSigningKey KeyOf(string clientId) => clientId switch
    {
        "partner-a" => ProofEnvironment.PartnerASigningKey,
        "partner-b" => ProofEnvironment.PartnerBSigningKey,
        _ => throw new ArgumentException(clientId)
    };

    // Every legitimate business call is signed with the calling Client's own key. Without a token there is nothing
    // the profile can bind to (the Authorization header is a signed component), so such a request stays unsigned.
    public static SignedCall Create(
        string clientId, Uri baseAddress, HttpMethod method, string target, string? accessToken, byte[]? body, SignOptions? options = null)
    {
        if (accessToken is null)
        {
            return new SignedCall { Method = method, Authority = baseAddress.Authority, PathAndQuery = target, Body = body };
        }

        return RequestSigner.Sign(method, baseAddress.Authority, target, accessToken, body, KeyOf(clientId), options);
    }

    public static SignedCall Submission(string clientId, Uri baseAddress, string? accessToken, string body = DemoBody)
        => Create(clientId, baseAddress, HttpMethod.Post, "/partner/submissions", accessToken, Encoding.UTF8.GetBytes(body));
}
