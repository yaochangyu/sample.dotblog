namespace Lab.Signature.WebApi.Tests.Support;

/// <summary>
/// Admin API 測試共用常數，對應 appsettings.Development.json 的 AdminApiKey 設定值。
/// </summary>
public static class AdminApiTestHelper
{
    public const string ValidAdminApiKey = "admin-dev-only-please-change";
    public const string InvalidAdminApiKey = "wrong-admin-key";
}
