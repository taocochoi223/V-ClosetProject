using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using VCloset.API.Hubs;
using VCloset.Application.DTOs.Chat.Responses;
using VCloset.Application.Interfaces;

namespace VCloset.API.Services;

public class ChatHubService : IChatHubService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatHubService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessageToRoomAsync(string roomIdString, ChatMessageResponseDto message)
    {
        // Gửi tin nhắn real-time tới tất cả client đang join group SignalR của room này
        await _hubContext.Clients.Group(roomIdString).SendAsync("ReceiveMessage", message);
    }
}
