using System.ComponentModel.DataAnnotations.Schema;
namespace VendingIot.Models;
public class VendingItem
{
    public int Id {get; set;}

    public int VendingMachineId{get; set;}

    public VendingMachine? VendingMachine{get; set;}

    public int ItemId{get; set;}
    public Item? Item{get; set;}

    public int Quantity{get; set;}

    public int Capacity {get; set;}

    public DateTime LastUpdated{get; set;}
}