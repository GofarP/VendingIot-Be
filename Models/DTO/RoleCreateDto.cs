public class RoleCreateDto
{
    public string? Id { get; set; } 
    public string Name { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new List<int>();
}