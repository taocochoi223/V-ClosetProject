using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace VCloset.API.Hubs;

public class ChatHub : Hub
{
    /// <summary>
    /// Tham gia phòng chat cụ thể (sẽ nhận các tin nhắn real-time đẩy về phòng này)
    /// </summary>
    public async Task JoinRoom(string roomIdString)
    {
        if (Guid.TryParse(roomIdString, out Guid roomId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        }
    }

    /// <summary>
    /// Rời phòng chat
    /// </summary>
    public async Task LeaveRoom(string roomIdString)
    {
        if (Guid.TryParse(roomIdString, out Guid roomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        }
    }
}
