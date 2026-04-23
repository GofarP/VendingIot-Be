using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VendingIot.Models;
[Index(nameof(Name), IsUnique =true)]

public class PermissionCategory
{
    public int Id{get; set;}

    public string Name{get; set;}

    public string Description {get;set;}
}