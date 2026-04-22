using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VendingIot.Models;

[Index(nameof(Name), IsUnique = true)]
public class Department
{
    public int Id { get; set; }

    [StringLength(100, ErrorMessage = "Nama jangan kepanjangan.")]
    public string Name { get; set; }

    [StringLength(100)]
    public string Description { get; set; }

}