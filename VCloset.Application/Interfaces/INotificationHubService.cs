using System.Threading.Tasks;
using VCloset.Application.DTOs;

namespace VCloset.Application.Interfaces;

public interface INotificationHubService
{
    Task SendUnreadCountAlertAsync(int userId, int unreadCount);
    Task SendNotificationAlertAsync(int userId, NotificationResponseDto notification);
}
