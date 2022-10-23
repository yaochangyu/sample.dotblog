using Microsoft.AspNetCore.Authentication;

namespace Lab.AspNetCore.Security.BasicAuthentication;

public class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Realm { get; set; }
}