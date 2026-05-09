using Microsoft.AspNetCore.Authorization;

namespace VendingIot.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission) { }
}