using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VendingIot.Data;
using VendingIot.Models;
using VendingIoT.Models;
using VendingIot.Models.DTO;
using VendingIoT.DTOs;
using VendingIoT.Helpers;

namespace VendingIot.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ITokenHelper _tokenHelper;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IConfiguration config,
        ITokenHelper tokenHelper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _config = config;
        _tokenHelper = tokenHelper;
    }

    public async Task<IdentityResult> RegisterAsync(RegisterDTO model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        return await _userManager.CreateAsync(user, model.Password);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDTO model, string? ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            return null;
        }

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(TokenRequestDTO model, string? ipAddress)
    {
        var principal = _tokenHelper.GetPrincipalFromExpiredToken(model.AccessToken);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var hashedInputToken = _tokenHelper.HashToken(model.RefreshToken);
        var savedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == hashedInputToken && x.UserId == userId);

        if (savedRefreshToken == null || !savedRefreshToken.IsActive)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        // Revoke token lama
        savedRefreshToken.Revoked = DateTime.UtcNow;
        savedRefreshToken.RevokedByIp = ipAddress;

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<bool> LogoutAsync(string refreshToken, string? ipAddress)
    {
        var hashedInput = _tokenHelper.HashToken(refreshToken);
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == hashedInput);

        if (tokenEntity == null)
        {
            return false;
        }

        tokenEntity.IsRevoked = true;
        tokenEntity.Revoked = DateTime.UtcNow;
        tokenEntity.RevokedByIp = ipAddress;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResponseDto?> GoogleLoginAsync(string idToken, string? ipAddress)
    {
        // 1. Validasi ID Token dari Google
        GoogleJsonWebSignature.Payload? payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["Google:ClientId"] }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException)
        {
            // Token tidak valid atau expired
            return null;
        }

        // 2. Cari user berdasarkan email
        var user = await _userManager.FindByEmailAsync(payload.Email);

        if (user == null)
        {
            // 3. Buat user baru jika belum terdaftar
            user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                FullName = payload.Name ?? payload.Email,
                EmailConfirmed = true, // Email dari Google sudah terverifikasi
                // Simpan foto profil Google (opsional)
                Photo = payload.Picture
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return null;
            }

            // Assign role default untuk user OAuth baru
            if (await _roleManager.RoleExistsAsync("User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
        }

        // 4. Generate JWT + Refresh Token (sama seperti login biasa)
        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user, string? ipAddress)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = new List<string>();
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var roleName in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, roleName));
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in roleClaims)
                {
                    authClaims.Add(claim);
                    if (!string.IsNullOrEmpty(claim.Value))
                    {
                        permissions.Add(claim.Value);
                    }
                }
            }
        }

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var duration = _config.GetValue<int>("Jwt:DurationInMinutes", 15);
        var expiration = DateTime.UtcNow.AddMinutes(duration);
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            expires: expiration,
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        var jwtString = new JwtSecurityTokenHandler().WriteToken(token);
        var plainRefreshToken = _tokenHelper.Generate();
        var hashedRefreshToken = _tokenHelper.HashToken(plainRefreshToken);

        var refreshTokenEntry = new RefreshToken
        {
            Token = hashedRefreshToken,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshTokenEntry);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = jwtString,
            RefreshToken = plainRefreshToken,
            Email = user.Email,
            FullName = user.FullName,
            PhotoUrl = user.Photo,
            Roles = roles,
            Permissions = permissions.Distinct().ToList(),
            ExpiresIn = duration * 60
        };
    }
}
