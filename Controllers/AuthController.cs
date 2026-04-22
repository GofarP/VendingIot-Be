using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Models;
using VendingIot.Models.DTO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VendingIot.Helpers;
namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    private readonly IConfiguration _config;
    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO model)
    {
        try
        {
            Validation.Required(ModelState, "Name", model.Email, "Please Fill The Email");
            Validation.Required(ModelState, "Password", model.Password, "Please Fill The Password");

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized(new { message = "Incorrect email or password" });

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            Console.WriteLine(model.Password);
            if (!isPasswordValid) return Unauthorized(new { message = "Incorrect email or passwords" });

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

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("JWT Key is not configured in appsettings.");
            }

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

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An error occurred during the login process. Please try again later."
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { message = "Sesi Tidak Valid" });
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return Ok(new { message = "Logged out (user not found)." });
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Gagal mengupdate status logout." });
        }

        return Ok(new { message = "Berhasil Logout" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            email = user.Email,
            fullName = user.FullName,
            roles = roles
        });
    }
}