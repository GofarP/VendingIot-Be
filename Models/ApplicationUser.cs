using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Identity;

namespace VendingIot.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    [Column(TypeName="varchar(255)")]
    public string? Photo {get; set;}
}   