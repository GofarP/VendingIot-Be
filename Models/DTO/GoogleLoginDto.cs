namespace VendingIot.Models.DTO;

public class GoogleLoginDto
{
    /// <summary>
    /// ID Token yang diterima dari Google Sign-In di frontend
    /// </summary>
    public string IdToken { get; set; } = string.Empty;
}
