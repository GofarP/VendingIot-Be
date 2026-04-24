using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace VendingIot.Models;

[Index(nameof(Name), IsUnique = true)]
public class ItemCategory
{
    public int Id { get; set; }
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(100)]
    public string Description { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();

}