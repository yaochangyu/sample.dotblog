using Microsoft.AspNetCore.Authentication;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Realm { get; set; }
}