using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Models;
using VendingIot.Models.DTO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using VendingIot.Helpers;
using VendingIoT.Models;
using VendingIot.Data;
using VendingIoT.Helpers;
using Microsoft.EntityFrameworkCore;
using VendingIoT.DTOs;

namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _config;

    private readonly IValidator<LoginDTO> _loginValidator;
    private readonly IValidator<RegisterDTO> _registerValidator;
    private readonly ITokenHelper _tokenHelper;

    private readonly ApplicationDbContext _context;

    public AuthController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        IValidator<LoginDTO> loginValidator,
        IValidator<RegisterDTO> registerValidator,
        ITokenHelper tokenHelper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _tokenHelper = tokenHelper;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO model)
    {
        // 1. Eksekusi Validasi Register
        var validationResult = await _registerValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation failed",
                errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return StatusCode(500, new
            {
                message = "Gagal membuat akun.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        // Opsional: Otomatis memberikan Role "User" ke pendaftar baru
        // await _userManager.AddToRoleAsync(user, "User");

        return Ok(new { message = "Registrasi berhasil! Silakan login." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO model)
    {
        try
        {
            var validation = await _loginValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation Failed",
                    errors = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new { message = "Incorrect email or password" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = new List<string>();
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
                        permissions.Add(claim.Value);
                    }
                }
            }

            var jwtKey = _config["Jwt:Key"] ?? throw new Exception("JWT Key is not configured.");
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
                Token = hashedRefreshToken, // <-- Simpan hasil gilingan
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.RefreshTokens.Add(refreshTokenEntry);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = jwtString,
                refreshToken = plainRefreshToken,
                email = user.Email,
                fullName = user.FullName,
                photo=user.Photo,
                roles = roles,
                permissions = permissions.Distinct().ToList(),
                expiresIn = duration * 60
            });

        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal error occurred." });
        }
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDTO model)
    {
        if (model == null || string.IsNullOrEmpty(model.RefreshToken))
        {
            return BadRequest(new { message = "Token is required" });
        }

        // --- LANGKAH PENTING: Giling dulu input dari user ---
        var hashedInput = _tokenHelper.HashToken(model.RefreshToken);

        // Cari pakai hasil gilingannya
        var refreshToken = await _context.RefreshTokens
            .Where(x => x.Token == hashedInput) // <-- Bandingkan Gilingan vs Gilingan
            .FirstOrDefaultAsync();

        if (refreshToken == null)
        {
            return NotFound(new { message = "Refresh token not found in database" });
        }

        // Kalau ketemu, matikan tokennya
        refreshToken.IsRevoked = true;
        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _context.SaveChangesAsync();

        return Ok(new { message = "Logged out and token revoked" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new { email = user.Email, fullName = user.FullName, roles = roles });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRequestDTO model)
    {
        if (model == null || string.IsNullOrEmpty(model.AccessToken) || string.IsNullOrEmpty(model.RefreshToken))
        {
            return BadRequest(new { message = "Invalid client request" });
        }

        try
        {
            var principal = _tokenHelper.GetPrincipalFromExpiredToken(model.AccessToken);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid Access Token" });
            }

            var hashedInputToken = _tokenHelper.HashToken(model.RefreshToken);

            var savedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == hashedInputToken && x.UserId == userId);

            if (savedRefreshToken == null || !savedRefreshToken.IsActive)
            {
                return Unauthorized(new { message = "Refresh token is invalid or has expired" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = new List<string>(); // Untuk menampung list permission string

            var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
                        permissions.Add(claim.Value);
                    }
                }
            }

            var jwtKey = _config["Jwt:Key"];
            var duration = _config.GetValue<int>("Jwt:DurationInMinutes", 15);
            var expiration = DateTime.UtcNow.AddMinutes(duration);
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));

            var newToken = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: expiration,
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
            );

            var newJwtString = new JwtSecurityTokenHandler().WriteToken(newToken);

            // Revoke token lama
            savedRefreshToken.Revoked = DateTime.UtcNow;

            var newPlainRefreshToken = _tokenHelper.Generate();
            var newHashedRefreshToken = _tokenHelper.HashToken(newPlainRefreshToken);

            var newRefreshTokenEntry = new RefreshToken
            {
                Token = newHashedRefreshToken,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.RefreshTokens.Add(newRefreshTokenEntry);
            await _context.SaveChangesAsync();

            // RETURN LENGKAP UNTUK FRONTEND
            return Ok(new
            {
                token = newJwtString,
                refreshToken = newPlainRefreshToken,
                expiresIn = duration * 60,

                // Tambahan data Profile
                email = user.Email,
                fullName = user.FullName, // Pastikan property FullName ada di ApplicationUser kamu
                roles = roles,
                permissions = permissions.Distinct().ToList() // Distinct agar tidak ada duplikat
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = "Token refresh failed" });
        }
    }
}