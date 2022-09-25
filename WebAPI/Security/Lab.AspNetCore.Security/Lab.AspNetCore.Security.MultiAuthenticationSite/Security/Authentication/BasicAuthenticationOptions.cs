using Microsoft.AspNetCore.Authentication;

namespace Lab.AspNetCore.Security.MultiAuthenticationSite.Security.Authentication;

public class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Realm { get; set; }
}