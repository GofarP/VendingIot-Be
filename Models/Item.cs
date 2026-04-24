using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace VendingIot.Models;

public class Item
{
    public int Id { get; set; }
    [Column(TypeName = "varchar(30)")]
    public string Name{get; set;}

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int Quantity{get; set;}

    public int ItemCategoryId{get;set;}

    public ItemCategory? ItemCategory{get;set;}
}