using System.Security.Claims;

namespace VendingIoT.Helpers
{
    public interface ITokenHelper
    {
        string Generate();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}