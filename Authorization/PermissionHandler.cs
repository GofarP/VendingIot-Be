using Microsoft.AspNetCore.Authorization;

namespace VendingIot.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Mencari klaim dengan tipe "Permission" yang nilainya sesuai
        var permissions = context.User.Claims
            .Where(x => x.Type == "Permission")
            .Select(x => x.Value);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}