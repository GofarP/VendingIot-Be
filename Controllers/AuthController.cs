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
    private readonly IConfiguration _config;
    
    private readonly IValidator<LoginDTO> _loginValidator;
    private readonly IValidator<RegisterDTO> _registerValidator;

    public AuthController(
        UserManager<ApplicationUser> userManager, 
        IConfiguration config,
        IValidator<LoginDTO> loginValidator,
        IValidator<RegisterDTO> registerValidator)
    {
        _userManager = userManager;
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
            var validationResult = await _loginValidator.ValidateAsync(model);
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

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized(new { message = "Incorrect email or password" });

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid) return Unauthorized(new { message = "Incorrect email or password" });

            var roles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var durationInMinutes = _config.GetValue<int>("Jwt:DurationInMinutes", 60);
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey)) throw new Exception("JWT Key is not configured.");

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: DateTime.Now.AddMinutes(durationInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            var jwtString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                tokenType = "Bearer",
                accessToken = jwtString,
                expiresIn = durationInMinutes * 60,
                email = user.Email,
                fullName = user.FullName,
                roles = roles
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Login Error] {DateTime.Now}: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during the login process." });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(new { message = "Sesi Tidak Valid" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Ok(new { message = "Logged out (user not found)." });

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(new { message = "Gagal mengupdate status logout." });

        return Ok(new { message = "Berhasil Logout" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        // ... (Kode GetCurrentUser sama seperti sebelumnya)
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new { email = user.Email, fullName = user.FullName, roles = roles });
    }
}