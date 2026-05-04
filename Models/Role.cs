using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace VendingIot.Models;

public class Role
{
    public int Id{get;set;}
    [StringLength(100)]
    public string Name{get;set;}
}