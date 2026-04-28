using System.Security.Claims;

namespace VendingIoT.Helpers
{
    public interface ITokenHelper
    {
        string Generate();
        string HashToken(string token); 
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}