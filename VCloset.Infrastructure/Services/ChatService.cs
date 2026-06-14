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

using VCloset.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace VCloset.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IChatHubService _chatHubService;
    private readonly VClosetVersion30Context _context;

    public ChatService(IUnitOfWork unitOfWork, IStorageService storageService, IChatHubService chatHubService, VClosetVersion30Context context)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _chatHubService = chatHubService;
        _context = context;
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
        var targetUsers = await _context.Users.Where(u => request.MemberUserIds.Contains(u.Id)).ToListAsync();
        foreach (var user in targetUsers)
        {
            var member = new ChatRoomMember
            {
                RoomInternalId = newRoom.InternalId,
                UserInternalId = user.InternalId,
                JoinedAt = DateTime.UtcNow
            };
            await _unitOfWork.ChatRoomMembers.AddAsync(member);
        }

        await _unitOfWork.SaveChangesAsync();

        return MapToRoomDto(newRoom, 0, null);
    }

    public async Task<ChatRoomResponseDto> UpdateGroupRoomAsync(int userId, Guid roomId, UpdateGroupRoomRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (user == null || room == null) throw new Exception("Không tìm thấy phòng chat.");

        if (room.RoomType != ChatRoomType.Topic) throw new Exception("Chỉ có thể cập nhật thông tin cho nhóm chat.");

        // Chỉ Admin nhóm mới được cập nhật
        if (room.CreatedByInternal != user.InternalId) throw new Exception("Bạn không có quyền cập nhật nhóm này.");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            room.Name = request.Name;
        }

        if (request.CoverUrl != null)
        {
            room.CoverUrl = request.CoverUrl; // Cập nhật hoặc xóa ảnh nếu url rỗng
        }

        _context.ChatRooms.Update(room);
        await _context.SaveChangesAsync();

        return MapToRoomDto(room, 0, null);
    }

    public async Task<bool> AddMembersToGroupAsync(int userId, Guid roomId, AddGroupMembersRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (user == null || room == null) return false;

        // Chỉ cho phép phòng chat nhóm (Topic)
        if (room.RoomType != ChatRoomType.Topic) return false;

        // Người thêm phải là thành viên trong nhóm
        var isMember = await _context.ChatRoomMembers.AnyAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (!isMember) throw new Exception("Bạn không phải là thành viên của phòng chat này.");

        // Lấy danh sách thành viên hiện tại để tránh thêm trùng
        var existingMemberIds = await _context.ChatRoomMembers
            .Where(m => m.RoomInternalId == room.InternalId)
            .Select(m => m.UserInternalId)
            .ToListAsync();

        var targetUsers = await _context.Users.Where(u => request.MemberUserIds.Contains(u.Id)).ToListAsync();
        bool addedAny = false;

        foreach (var tUser in targetUsers)
        {
            if (!existingMemberIds.Contains(tUser.InternalId))
            {
                var newMember = new ChatRoomMember
                {
                    RoomInternalId = room.InternalId,
                    UserInternalId = tUser.InternalId,
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.ChatRoomMembers.AddAsync(newMember);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<List<ChatRoomMemberResponseDto>> GetRoomMembersAsync(int userId, Guid roomId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (user == null || room == null) return new List<ChatRoomMemberResponseDto>();

        // Chỉ cho xem nếu người này đang trong phòng
        var isMember = await _context.ChatRoomMembers.AnyAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (!isMember) throw new Exception("Bạn không phải là thành viên của phòng chat này.");

        var members = await _context.ChatRoomMembers
            .Include(m => m.UserInternal)
            .Where(m => m.RoomInternalId == room.InternalId)
            .Select(m => new ChatRoomMemberResponseDto
            {
                UserId = m.UserInternal.Id,
                DisplayName = m.UserInternal.DisplayName,
                Email = m.UserInternal.Email,
                AvatarUrl = m.UserInternal.AvatarUrl,
                JoinedAt = m.JoinedAt,
                IsAdmin = (room.CreatedByInternal == m.UserInternal.InternalId)
            })
            .ToListAsync();

        return members;
    }

    public async Task<bool> RemoveMemberFromGroupAsync(int userId, Guid roomId, Guid targetUserId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (user == null || room == null || targetUser == null) return false;

        if (room.RoomType != ChatRoomType.Topic) throw new Exception("Chỉ có thể kích thành viên trong nhóm chat.");

        // Kiểm tra quyền Admin (người tạo nhóm)
        if (room.CreatedByInternal != user.InternalId)
        {
            throw new Exception("Bạn không có quyền xoá thành viên khỏi nhóm.");
        }

        // Không thể tự kích chính mình bằng API này (dùng API Leave thay thế)
        if (user.InternalId == targetUser.InternalId)
        {
            throw new Exception("Bạn không thể tự kích chính mình.");
        }

        var memberToRemove = await _context.ChatRoomMembers
            .FirstOrDefaultAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == targetUser.InternalId);

        if (memberToRemove == null) return false;

        _context.ChatRoomMembers.Remove(memberToRemove);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<ChatRoomResponseDto>> GetChatRoomsAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return new List<ChatRoomResponseDto>();

        // Tìm tất cả các phòng chat của user này và nạp thông tin cần thiết bằng batch query để tránh N+1
        var userMemberships = await _context.ChatRoomMembers
            .Where(m => m.UserInternalId == user.InternalId)
            .ToListAsync();

        var roomInternalIds = userMemberships.Select(m => m.RoomInternalId).ToList();
        var roomsData = await _context.ChatRooms
            .Where(r => roomInternalIds.Contains(r.InternalId) && r.IsActive)
            .ToListAsync();

        // Lấy tin nhắn cuối cùng của mỗi phòng
        var lastMessages = await _context.ChatMessages
            .Where(msg => roomInternalIds.Contains(msg.RoomInternalId) && msg.DeletedAt == null)
            .GroupBy(msg => msg.RoomInternalId)
            .Select(g => g.OrderByDescending(m => m.SentAt).FirstOrDefault())
            .ToListAsync();

        // Lấy thông tin đối phương (thành viên khác) cho các phòng chat 1-1
        var directRoomIds = roomsData.Where(r => r.RoomType == ChatRoomType.Direct).Select(r => r.InternalId).ToList();
        
        var otherMembersData = await (from m in _context.ChatRoomMembers
                                      where directRoomIds.Contains(m.RoomInternalId) && m.UserInternalId != user.InternalId
                                      join u in _context.Users on m.UserInternalId equals u.InternalId
                                      select new { m.RoomInternalId, User = u }).ToListAsync();

        // Đếm số lượng tin nhắn chưa đọc cho mỗi phòng (so với LastReadAt)
        var unreadCounts = await (from msg in _context.ChatMessages
                                  join m in _context.ChatRoomMembers on msg.RoomInternalId equals m.RoomInternalId
                                  where m.UserInternalId == user.InternalId 
                                        && msg.DeletedAt == null 
                                        && msg.SentAt > (m.LastReadAt ?? m.JoinedAt)
                                  group msg by msg.RoomInternalId into g
                                  select new { RoomId = g.Key, Count = g.Count() })
                                  .ToDictionaryAsync(x => x.RoomId, x => x.Count);

        var result = new List<ChatRoomResponseDto>();

        foreach (var room in roomsData)
        {
            var lastMsg = lastMessages.FirstOrDefault(m => m != null && m.RoomInternalId == room.InternalId);
            
            string? displayName = room.Name;
            string? displayCover = room.CoverUrl;

            if (room.RoomType == ChatRoomType.Direct)
            {
                var otherMember = otherMembersData.FirstOrDefault(m => m.RoomInternalId == room.InternalId);
                if (otherMember != null && otherMember.User != null)
                {
                    displayName = otherMember.User.DisplayName;
                    displayCover = otherMember.User.AvatarUrl;
                }
            }

            var unread = unreadCounts.ContainsKey(room.InternalId) ? unreadCounts[room.InternalId] : 0;
            var dto = MapToRoomDto(room, unread, lastMsg);
            dto.Name = displayName;
            dto.CoverUrl = displayCover;

            result.Add(dto);
        }

        return result.OrderByDescending(r => r.LastMessageSentAt ?? r.CreatedAt).ToList();
    }

    public async Task<List<ChatMessageResponseDto>> GetRoomMessagesAsync(int userId, Guid roomId, int page, int pageSize)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (user == null || room == null) throw new Exception("Dữ liệu không hợp lệ.");

        // Xác thực người dùng có thuộc phòng chat này không
        var isMember = await _context.ChatRoomMembers.AnyAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (!isMember) throw new Exception("Bạn không có quyền xem tin nhắn của phòng chat này.");

        // Phân trang bằng DB (IQueryable) để tránh Memory Leak
        var pagedMessages = await _context.ChatMessages
            .Where(m => m.RoomInternalId == room.InternalId && m.DeletedAt == null)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lấy hàng loạt thông tin User và Outfit để chống N+1
        var userInternalIds = pagedMessages.Select(m => m.UserInternalId).Distinct().ToList();
        var senders = await _context.Users.Where(u => userInternalIds.Contains(u.InternalId)).ToListAsync();

        var outfitInternalIds = pagedMessages.Where(m => m.OutfitInternalId.HasValue).Select(m => m.OutfitInternalId!.Value).Distinct().ToList();
        var outfits = outfitInternalIds.Any() 
            ? await _context.CanvasOutfits.Where(o => outfitInternalIds.Contains(o.InternalId)).ToListAsync()
            : new List<CanvasOutfit>();

        var result = new List<ChatMessageResponseDto>();
        foreach (var msg in pagedMessages)
        {
            var sender = senders.FirstOrDefault(u => u.InternalId == msg.UserInternalId);
            var outfit = msg.OutfitInternalId.HasValue ? outfits.FirstOrDefault(o => o.InternalId == msg.OutfitInternalId.Value) : null;
            
            var dto = new ChatMessageResponseDto
            {
                Id = msg.Id,
                RoomId = room.Id,
                SenderId = sender?.Id ?? Guid.Empty,
                SenderName = sender?.DisplayName ?? "Người dùng ẩn danh",
                SenderAvatarUrl = sender?.AvatarUrl,
                Content = msg.Content,
                ImageUrl = msg.ImageUrl,
                MessageType = msg.MessageType,
                SentAt = msg.SentAt
            };

            if (outfit != null)
            {
                dto.OutfitId = outfit.Id;
                dto.OutfitName = outfit.Title;
                dto.OutfitImageUrl = outfit.CanvasSnapshotUrl;
            }

            result.Add(dto);
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

    public async Task<bool> MarkMessagesAsReadAsync(int userId, Guid roomId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (user == null || room == null) return false;

        var member = await _context.ChatRoomMembers.FirstOrDefaultAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (member == null) return false;

        member.LastReadAt = DateTime.UtcNow;
        _context.ChatRoomMembers.Update(member);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RecallMessageAsync(int userId, Guid messageId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var message = await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageId);

        if (user == null || message == null) return false;

        // Chỉ cho phép thu hồi tin nhắn do chính user gửi và chưa bị xóa
        if (message.UserInternalId != user.InternalId || message.DeletedAt != null)
        {
            return false;
        }

        message.DeletedAt = DateTime.UtcNow;
        _context.ChatMessages.Update(message);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LeaveGroupRoomAsync(int userId, Guid roomId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (user == null || room == null) return false;

        // Chỉ cho phép thoát khỏi phòng chat nhóm (Topic), không áp dụng cho chat 1-1 (Direct)
        if (room.RoomType != ChatRoomType.Topic)
        {
            throw new Exception("Không thể thoát khỏi phòng chat 1-1.");
        }

        var member = await _context.ChatRoomMembers.FirstOrDefaultAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (member == null) return false;

        // Xóa thành viên khỏi nhóm
        _context.ChatRoomMembers.Remove(member);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleMuteRoomAsync(int userId, Guid roomId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (user == null || room == null) return false;

        var member = await _context.ChatRoomMembers.FirstOrDefaultAsync(m => m.RoomInternalId == room.InternalId && m.UserInternalId == user.InternalId);
        if (member == null) return false;

        // Đảo ngược trạng thái Mute
        member.IsMuted = !member.IsMuted;
        
        _context.ChatRoomMembers.Update(member);
        await _context.SaveChangesAsync();

        return member.IsMuted;
    }
}
