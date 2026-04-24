using System.ComponentModel.DataAnnotations.Schema;

namespace VendingIot.Models;

public class VendingMachine
{
    public int Id { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string MachineCode { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string Name { get; set; }

    [Column(TypeName = "varchar(200)")]
    public string Location { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime LastRestock{get;set;}

    public ICollection<VendingItem> VendingItems { get; set; } = new List<VendingItem>();
}