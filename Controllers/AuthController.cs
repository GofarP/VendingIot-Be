using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Models;
using VendingIot.Models.DTO;
using VendingIot.Services;
using VendingIoT.DTOs;

namespace VendingIot.Controllers;

public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthService _authService;
    private readonly IValidator<LoginDTO> _loginValidator;
    private readonly IValidator<RegisterDTO> _registerValidator;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IAuthService authService,
        IValidator<LoginDTO> loginValidator,
        IValidator<RegisterDTO> registerValidator)
    {
        _userManager = userManager;
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO model)
    {
        var validationResult = await _registerValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        var result = await _authService.RegisterAsync(model);
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Gagal membuat akun.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        return Ok(new { message = "Registrasi berhasil! Silakan login." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO model)
    {
        var validation = await _loginValidator.ValidateAsync(model);
        if (!validation.IsValid)
        {
            return ValidationBadRequest(validation);
        }

        var response = await _authService.LoginAsync(model, GetIpAddress());
        if (response == null)
        {
            return Unauthorized(new { message = "Incorrect email or password" });
        }

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDTO model)
    {
        if (model == null || string.IsNullOrEmpty(model.RefreshToken))
        {
            return BadRequest(new { message = "Token is required" });
        }

        var success = await _authService.LogoutAsync(model.RefreshToken, GetIpAddress());
        if (!success)
        {
            return NotFound(new { message = "Refresh token not found in database" });
        }

        return Ok(new { message = "Logged out and token revoked" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            email = user.Email,
            fullName = user.FullName,
            roles,
            photo = user.Photo
        });
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
            var response = await _authService.RefreshTokenAsync(model, GetIpAddress());
            if (response == null)
            {
                return Unauthorized(new { message = "Refresh token is invalid or has expired" });
            }

            return Ok(response);
        }
        catch (Exception)
        {
            return Unauthorized(new { message = "Token refresh failed" });
        }
    }

    /// <summary>
    /// Login menggunakan Google OAuth (ID Token dari frontend Google Sign-In)
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto model)
    {
        if (string.IsNullOrEmpty(model.IdToken))
        {
            return BadRequest(new { message = "Google ID Token wajib diisi" });
        }

        var response = await _authService.GoogleLoginAsync(model.IdToken, GetIpAddress());
        if (response == null)
        {
            return Unauthorized(new { message = "Google token tidak valid atau sudah kedaluwarsa" });
        }

        return Ok(response);
    }
}