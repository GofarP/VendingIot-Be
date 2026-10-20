namespace VendingIot.Models.DTO;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PhotoUrl { get; set; }
    public IList<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public int ExpiresIn { get; set; }
}
