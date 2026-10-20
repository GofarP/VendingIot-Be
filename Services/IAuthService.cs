using Microsoft.AspNetCore.Identity;
using VendingIot.Models.DTO;
using VendingIoT.DTOs;

namespace VendingIot.Services;

public interface IAuthService
{
    Task<IdentityResult> RegisterAsync(RegisterDTO model);
    Task<AuthResponseDto?> LoginAsync(LoginDTO model, string? ipAddress);
    Task<AuthResponseDto?> RefreshTokenAsync(TokenRequestDTO model, string? ipAddress);
    Task<bool> LogoutAsync(string refreshToken, string? ipAddress);
    Task<AuthResponseDto?> GoogleLoginAsync(string idToken, string? ipAddress);
}
