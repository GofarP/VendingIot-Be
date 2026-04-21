using Microsoft.AspNetCore.Identity;

namespace VendingIot.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}