namespace Lab.CSRF.WebApi.Services;

public interface ICsrfTokenService
{
    /// <summary>
    /// 產生新的 CSRF Token
    /// </summary>
    /// <returns>Token 字串</returns>
    string GenerateToken();

    /// <summary>
    /// 驗證 CSRF Token 是否有效
    /// </summary>
    /// <param name="token">要驗證的 Token</param>
    /// <returns>是否有效</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// 移除 Token（登出或過期時使用）
    /// </summary>
    /// <param name="token">要移除的 Token</param>
    void RemoveToken(string token);
}
