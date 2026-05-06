namespace VendingIot.Models.DTO;

public class UserUpdateDTO
{
    public string Id {get;set;}
    public string FullName { get; set; }
    public string Email { get; set; }
    public string? Password { get; set; }

    public string? RoleName{get;set;}
    public IFormFile? PhotoFile { get; set; }
}