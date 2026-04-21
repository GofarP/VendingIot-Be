using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Models;
using VendingIot.Models.DTO;

namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser>_userManager;
    private readonly SignInManager<ApplicationUser>_signInManager;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager=userManager;
        _signInManager=signInManager;
    }

    [HttpPost("login")]
    public async Task <IActionResult> Login([FromBody] LoginDTO model)
    {
        var user=await _userManager.FindByEmailAsync(model.Email);
        if(user==null) return Unauthorized(new { message = "Incorrect email or password" });

        var result=await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure:false);

        if (result.Succeeded)
        {
            var roles=await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                message="Login Berhasil",
                email=user.Email,
                fullName=user.FullName,
                roles=roles   
            });
        }

        return Unauthorized(new { message = "Incorrect email or password." });
    }

    [HttpPost("logout")]
    [Authorize]
    public  async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new {message="Berhasil Logout"});
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user=await _userManager.GetUserAsync(User);
        if(user==null)return NotFound();

        var roles=await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            email = user.Email,
            fullName = user.FullName,
            roles = roles
        });
    }
}