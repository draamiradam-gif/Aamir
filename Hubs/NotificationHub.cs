using Microsoft.AspNetCore.SignalR;

public class NotificationHub : Hub
{
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    }

    public async Task SendAdminNotification(string message, string type = "info")
    {
        await Clients.Group("Admins").SendAsync("ReceiveNotification", message, type);
    }
}