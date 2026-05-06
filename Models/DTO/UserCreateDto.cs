namespace VendingIot.Models.DTO;

public class UserCreateDto
{
    public string FullName {get; set;}
    public string Email {get; set;}

    public string Password {get; set;}

    public string RoleId {get;set;}

    public IFormFile? Photo {get;set;}

}