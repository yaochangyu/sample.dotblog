namespace WebApiCore31
{
    public interface IJwtAuthenticationProvider
    {
        string Authenticate(string username, string password);
    }
}