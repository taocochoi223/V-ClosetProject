using System.Threading.Tasks;
using VCloset.Application.DTOs.Chat.Responses;

namespace VCloset.Application.Interfaces;

public interface IChatHubService
{
    Task SendMessageToRoomAsync(string roomIdString, ChatMessageResponseDto message);
}
