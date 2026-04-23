using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VendingIot.Models;

public class Permission
{
    public int Id { get; set; }

    [MaxLength(30, ErrorMessage = "Nama maksimal 30 karakter.")]
    [Column(TypeName = "varchar(30)")]
    public string Name { get; set; }
    public int PermissionCategoryId { get; set; }

    public PermissionCategory? PermissionCategory { get; set; }
}