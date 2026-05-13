public class UpdateProfileDTO
{
    public string FullName{get; set;}=string.Empty;

    public string Email{get; set;}=string.Empty;

    public IFormFile? Photo {get; set;}
}