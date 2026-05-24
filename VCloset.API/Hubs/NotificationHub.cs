using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;

namespace VCloset.API.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userIdStr = httpContext?.Request.Query["userId"].ToString();

        if (int.TryParse(userIdStr, out int userId))
        {
            // Thêm kết nối này vào Group riêng của user đó
            await Groups.AddToGroupAsync(Context.ConnectionId, $"UserGroup_{userId}");
            Console.WriteLine($"\n[SIGNALR CONNECT] User {userId} connected successfully. ConnectionId: {Context.ConnectionId}\n");
        }
        else
        {
            Console.WriteLine($"\n[SIGNALR CONNECT] Anonymous or invalid user connected. ConnectionId: {Context.ConnectionId}\n");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        var userIdStr = httpContext?.Request.Query["userId"].ToString();

        if (int.TryParse(userIdStr, out int userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"UserGroup_{userId}");
            Console.WriteLine($"\n[SIGNALR DISCONNECT] User {userId} disconnected. ConnectionId: {Context.ConnectionId}\n");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
