using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Chat.Requests;
using VCloset.Application.DTOs.Chat.Responses;

namespace VCloset.Application.Interfaces;

public interface IChatService
{
    // Rooms
    Task<ChatRoomResponseDto> CreateDirectRoomAsync(int userId, CreateDirectRoomRequest request);
    Task<ChatRoomResponseDto> CreateGroupRoomAsync(int userId, CreateGroupRoomRequest request);
    Task<bool> AddMembersToGroupAsync(int userId, Guid roomId, AddGroupMembersRequest request);
    Task<List<ChatRoomResponseDto>> GetChatRoomsAsync(int userId);
    Task<bool> LeaveGroupRoomAsync(int userId, Guid roomId);
    Task<bool> MarkMessagesAsReadAsync(int userId, Guid roomId);

    // Messages
    Task<List<ChatMessageResponseDto>> GetRoomMessagesAsync(int userId, Guid roomId, int page, int pageSize);
    Task<ChatMessageResponseDto> SendTextMessageAsync(int userId, Guid roomId, SendTextMessageRequest request);
    Task<ChatMessageResponseDto> SendImageMessageAsync(int userId, Guid roomId, IFormFile imageFile);
    Task<ChatMessageResponseDto> SendOutfitMessageAsync(int userId, Guid roomId, SendOutfitMessageRequest request);
    Task<bool> RecallMessageAsync(int userId, Guid messageId);
}
