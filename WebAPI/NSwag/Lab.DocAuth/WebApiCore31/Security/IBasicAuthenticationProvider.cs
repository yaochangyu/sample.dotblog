using System.Threading.Tasks;

namespace WebApiCore31.Security
{
    public interface IBasicAuthenticationProvider
    {
        Task<bool> Authenticate(string username, string password);
    }
}