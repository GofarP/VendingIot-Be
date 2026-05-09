using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VendingIot.Models;

namespace VendingIot.Hubs;

public class NotificationHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationHub(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, role);
                }
            }
        }
        await base.OnConnectedAsync();
    }

}