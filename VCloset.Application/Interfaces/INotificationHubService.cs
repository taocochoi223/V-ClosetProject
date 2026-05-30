using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

public interface INotificationHubService
{
    Task SendUnreadCountAlertAsync(int userId, int unreadCount);
    Task SendForceLogoutAsync(int userId);
}
