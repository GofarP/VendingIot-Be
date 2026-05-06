namespace VendingIot.Models.DTO;

public class UserCreateDto
{
    public string FullName {get; set;}
    public string Email {get; set;}

    public string Password {get; set;}

    public string RoleName {get;set;}=string.Empty;

    public IFormFile? PhotoFile {get;set;}

}