using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VCloset.Application.Interfaces;
using VCloset.Domain.Enums;

namespace VCloset.API.Services;

public class PaymentCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentCleanupService> _logger;

    public PaymentCleanupService(IServiceProvider serviceProvider, ILogger<PaymentCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredPaymentsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Payment Cleanup.");
            }

            // Dọn rác mỗi 5 phút một lần
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task CleanupExpiredPaymentsAsync()
    {
        // BackgroundService là Singleton, nên phải tạo Scope để gọi Scoped Service như IUnitOfWork
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Tìm các đơn hàng đã tạo quá 15 phút nhưng vẫn đang Pending (Chờ)
        var expirationThreshold = DateTime.UtcNow.AddMinutes(-10);
        
        var pendingTransactions = await unitOfWork.PaymentTransactions.FindAllAsync(pt => 
            pt.Status == PaymentStatus.Pending && 
            pt.CreatedAt < expirationThreshold);

        var transactionsList = pendingTransactions.ToList();
        if (transactionsList.Any())
        {
            foreach (var transaction in transactionsList)
            {
                transaction.Status = PaymentStatus.Expired;
                transaction.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation($"Transaction {transaction.Id} marked as Expired.");
            }

            await unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Cleaned up {transactionsList.Count} expired transactions.");
        }
    }
}
