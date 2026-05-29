using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Chat.Requests;
using VCloset.Application.DTOs.Chat.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IChatHubService _chatHubService;

    public ChatService(IUnitOfWork unitOfWork, IStorageService storageService, IChatHubService chatHubService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _chatHubService = chatHubService;
    }

    public async Task<ChatRoomResponseDto> CreateDirectRoomAsync(int userId, CreateDirectRoomRequest request)
    {
        var currentUser = await _unitOfWork.Users.GetByIdAsync(userId);
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == request.TargetUserId);

        if (currentUser == null || targetUser == null)
        {
            throw new Exception("Người dùng không hợp lệ.");
        }

        // Tìm xem đã có phòng chat 1-1 giữa 2 người này chưa
        var currentMemberRooms = await _unitOfWork.ChatRoomMembers.FindAllAsync(m => m.UserInternalId == currentUser.InternalId);
        var targetMemberRooms = await _unitOfWork.ChatRoomMembers.FindAllAsync(m => m.UserInternalId == targetUser.InternalId);

        var commonRoomId = currentMemberRooms
            .Select(m => m.RoomInternalId)
            .Intersect(targetMemberRooms.Select(m => m.RoomInternalId))
            .FirstOrDefault();

        if (commonRoomId != 0)
        {
            var existingRoom = await _unitOfWork.ChatRooms.FindAsync(r => r.InternalId == commonRoomId && r.RoomType == ChatRoomType.Direct);
            if (existingRoom != null)
            {
                return MapToRoomDto(existingRoom, 0, null);
            }
        }

        // Chưa có thì tạo phòng mới
        var newRoom = new ChatRoom
        {
            Id = Guid.NewGuid(),
            RoomType = ChatRoomType.Direct,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByInternal = currentUser.InternalId
        };

        await _unitOfWork.ChatRooms.AddAsync(newRoom);
        await _unitOfWork.SaveChangesAsync(); // Lưu để sinh InternalId thực tế

        // Add 2 thành viên
        var member1 = new ChatRoomMember
        {
            RoomInternalId = newRoom.InternalId,
            UserInternalId = currentUser.InternalId,
            JoinedAt = DateTime.UtcNow
        };

        var member2 = new ChatRoomMember
        {
            RoomInternalId = newRoom.InternalId,
            UserInternalId = targetUser.InternalId,
            JoinedAt = DateTime.UtcNow
        };

        await _unitOfWork.ChatRoomMembers.AddAsync(member1);
        await _unitOfWork.ChatRoomMembers.AddAsync(member2);
        await _unitOfWork.SaveChangesAsync();

        return MapToRoomDto(newRoom, 0, null);
    }

    public async Task<ChatRoomResponseDto> CreateGroupRoomAsync(int userId, CreateGroupRoomRequest request)
    {
        var currentUser = await _unitOfWork.Users.GetByIdAsync(userId);
        if (currentUser == null) throw new Exception("Không tìm thấy người dùng hiện tại.");

        var newRoom = new ChatRoom
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CoverUrl = request.CoverUrl,
            RoomType = ChatRoomType.Topic,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByInternal = currentUser.InternalId
        };

        await _unitOfWork.ChatRooms.AddAsync(newRoom);
        await _unitOfWork.SaveChangesAsync(); // Sinh InternalId

        // Add Admin (người tạo)
        var adminMember = new ChatRoomMember
        {
            RoomInternalId = newRoom.InternalId,
            UserInternalId = currentUser.InternalId,
            JoinedAt = DateTime.UtcNow
        };
        await _unitOfWork.ChatRoomMembers.AddAsync(adminMember);

        // Add các thành viên khác
        foreach (var memberId in request.MemberUserIds)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.Id == memberId);
            if (user != null)
            {
                var member = new ChatRoomMember
                {
                    RoomInternalId = newRoom.InternalId,
                    UserInternalId = user.InternalId,
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.ChatRoomMembers.AddAsync(member);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return MapToRoomDto(newRoom, 0, null);
    }

    public async Task<List<ChatRoomResponseDto>> GetChatRoomsAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return new List<ChatRoomResponseDto>();

        // Tìm tất cả các phòng chat của user này
        var memberships = await _unitOfWork.ChatRoomMembers.FindAllAsync(m => m.UserInternalId == user.InternalId);
        var roomIds = memberships.Select(m => m.RoomInternalId).ToList();

        var result = new List<ChatRoomResponseDto>();

        foreach (var roomId in roomIds)
        {
            var room = await _unitOfWork.ChatRooms.FindAsync(r => r.InternalId == roomId && r.IsActive);
            if (room != null)
            {
                // Lấy tin nhắn cuối cùng
                var messages = await _unitOfWork.ChatMessages.FindAllAsync(m => m.RoomInternalId == roomId && m.DeletedAt == null);
                var lastMsg = messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

                // Lấy tên hiển thị cho phòng chat Direct dựa vào đối phương
                string? displayName = room.Name;
                string? displayCover = room.CoverUrl;

                if (room.RoomType == ChatRoomType.Direct)
                {
                    var otherMembers = await _unitOfWork.ChatRoomMembers.FindAllAsync(m => m.RoomInternalId == roomId && m.UserInternalId != user.InternalId);
                    var otherMember = otherMembers.FirstOrDefault();
                    if (otherMember != null)
                    {
                        var otherUser = await _unitOfWork.Users.FindAsync(u => u.InternalId == otherMember.UserInternalId);
                        if (otherUser != null)
                        {
                            displayName = otherUser.DisplayName;
                            displayCover = otherUser.AvatarUrl;
                        }
                    }
                }

                var dto = MapToRoomDto(room, 0, lastMsg);
                dto.Name = displayName;
                dto.CoverUrl = displayCover;

                result.Add(dto);
            }
        }

        return result.OrderByDescending(r => r.LastMessageSentAt ?? r.CreatedAt).ToList();
    }

    public async Task<List<ChatMessageResponseDto>> GetRoomMessagesAsync(int userId, Guid roomId, int page, int pageSize)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _unitOfWork.ChatRooms.FindAsync(r => r.Id == roomId);

        if (user == null || room == null) throw new Exception("Dữ liệu không hợp lệ.");

        // Xác thực người dùng có thuộc phòng chat này không
        var isMember = await _unitOfWork.ChatRoomMembers.FindAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (isMember == null) throw new Exception("Bạn không có quyền xem tin nhắn của phòng chat này.");

        var messages = await _unitOfWork.ChatMessages.FindAllAsync(m => m.RoomInternalId == room.InternalId && m.DeletedAt == null);

        var pagedMessages = messages
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new List<ChatMessageResponseDto>();
        foreach (var msg in pagedMessages)
        {
            var sender = await _unitOfWork.Users.FindAsync(u => u.InternalId == msg.UserInternalId);
            result.Add(await MapToMessageDtoAsync(msg, room.Id, sender));
        }

        return result;
    }

    public async Task<ChatMessageResponseDto> SendTextMessageAsync(int userId, Guid roomId, SendTextMessageRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _unitOfWork.ChatRooms.FindAsync(r => r.Id == roomId);

        if (user == null || room == null) throw new Exception("Dữ liệu không hợp lệ.");

        // Xác thực thành viên
        var isMember = await _unitOfWork.ChatRoomMembers.FindAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (isMember == null) throw new Exception("Bạn không phải thành viên phòng chat này.");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomInternalId = room.InternalId,
            UserInternalId = user.InternalId,
            Content = request.Content,
            MessageType = MessageType.Text,
            SentAt = DateTime.UtcNow
        };

        await _unitOfWork.ChatMessages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapToMessageDtoAsync(message, room.Id, user);

        // Bắn SignalR real-time đến tất cả các thành viên đang online trong phòng chat
        await _chatHubService.SendMessageToRoomAsync(room.Id.ToString(), dto);

        return dto;
    }

    public async Task<ChatMessageResponseDto> SendImageMessageAsync(int userId, Guid roomId, IFormFile imageFile)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _unitOfWork.ChatRooms.FindAsync(r => r.Id == roomId);

        if (user == null || room == null) throw new Exception("Dữ liệu không hợp lệ.");

        // Xác thực thành viên
        var isMember = await _unitOfWork.ChatRoomMembers.FindAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (isMember == null) throw new Exception("Bạn không phải thành viên phòng chat này.");

        if (imageFile == null || imageFile.Length == 0)
        {
            throw new Exception("File ảnh không hợp lệ.");
        }

        // Upload hình ảnh lên Storage
        using var stream = imageFile.OpenReadStream();
        var fileName = $"chat-images/room_{room.Id}/user_{user.Id}_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
        var imageUrl = await _storageService.UploadFileAsync(stream, fileName, imageFile.ContentType);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomInternalId = room.InternalId,
            UserInternalId = user.InternalId,
            ImageUrl = imageUrl,
            MessageType = MessageType.Image,
            SentAt = DateTime.UtcNow
        };

        await _unitOfWork.ChatMessages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapToMessageDtoAsync(message, room.Id, user);

        // Bắn SignalR
        await _chatHubService.SendMessageToRoomAsync(room.Id.ToString(), dto);

        return dto;
    }

    public async Task<ChatMessageResponseDto> SendOutfitMessageAsync(int userId, Guid roomId, SendOutfitMessageRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _unitOfWork.ChatRooms.FindAsync(r => r.Id == roomId);
        var outfit = await _unitOfWork.CanvasOutfits.FindAsync(o => o.Id == request.OutfitId);

        if (user == null || room == null || outfit == null) throw new Exception("Dữ liệu không hợp lệ.");

        // Xác thực thành viên
        var isMember = await _unitOfWork.ChatRoomMembers.FindAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (isMember == null) throw new Exception("Bạn không phải thành viên phòng chat này.");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomInternalId = room.InternalId,
            UserInternalId = user.InternalId,
            OutfitInternalId = outfit.InternalId,
            MessageType = MessageType.OutfitShare,
            SentAt = DateTime.UtcNow
        };

        await _unitOfWork.ChatMessages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapToMessageDtoAsync(message, room.Id, user);

        // Bắn SignalR
        await _chatHubService.SendMessageToRoomAsync(room.Id.ToString(), dto);

        return dto;
    }

    private static ChatRoomResponseDto MapToRoomDto(ChatRoom entity, int unreadCount, ChatMessage? lastMsg)
    {
        return new ChatRoomResponseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CoverUrl = entity.CoverUrl,
            RoomType = entity.RoomType,
            CreatedAt = entity.CreatedAt,
            UnreadCount = unreadCount,
            LastMessageContent = lastMsg?.MessageType == MessageType.Image ? "[Hình ảnh]" : 
                                 lastMsg?.MessageType == MessageType.OutfitShare ? "[Chia sẻ bộ đồ]" : 
                                 lastMsg?.Content,
            LastMessageSentAt = lastMsg?.SentAt
        };
    }

    private async Task<ChatMessageResponseDto> MapToMessageDtoAsync(ChatMessage entity, Guid roomId, User? sender)
    {
        var dto = new ChatMessageResponseDto
        {
            Id = entity.Id,
            RoomId = roomId,
            SenderId = sender?.Id ?? Guid.Empty,
            SenderName = sender?.DisplayName ?? "Người dùng ẩn danh",
            SenderAvatarUrl = sender?.AvatarUrl,
            Content = entity.Content,
            ImageUrl = entity.ImageUrl,
            MessageType = entity.MessageType,
            SentAt = entity.SentAt
        };

        if (entity.OutfitInternalId.HasValue)
        {
            var outfit = await _unitOfWork.CanvasOutfits.FindAsync(o => o.InternalId == entity.OutfitInternalId.Value);
            if (outfit != null)
            {
                dto.OutfitId = outfit.Id;
                dto.OutfitName = outfit.Title;
                dto.OutfitImageUrl = outfit.CanvasSnapshotUrl;
            }
        }

        return dto;
    }
}
