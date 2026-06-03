using System.Threading.Tasks;
using VCloset.Application.DTOs;

namespace VCloset.Application.Interfaces;

public interface INotificationHubService
{
    Task SendUnreadCountAlertAsync(int userId, int unreadCount);
    Task SendForceLogoutAsync(int userId);
    Task SendNotificationAlertAsync(int userId, NotificationResponseDto notification);
    Task SendPaymentUpdateAsync(int userId, object paymentUpdate);
    Task SendPendingPaymentAlertAsync(object pendingPayment);
}
