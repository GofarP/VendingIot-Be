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

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        IValidator<LoginDTO> loginValidator,
        IValidator<RegisterDTO> registerValidator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
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
            var duration = _config.GetValue<int>("Jwt:DurationInMinutes", 60);
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
            Response.Cookies.Append("vending_token", jwtString, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/"
            });

            return Ok(new
            {
                token = jwtString,
                email = user.Email,
                fullName = user.FullName,
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
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("vending_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        return Ok(new { message = "Berhasil Logout" });
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
}