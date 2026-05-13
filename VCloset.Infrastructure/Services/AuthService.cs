using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;

namespace VCloset.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;
    private readonly IEmailService _emailService;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IDistributedCache cache,
        IEmailService emailService,
        IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _emailService = emailService;
        _jwtService = jwtService;
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified) 
                throw new InvalidOperationException("EMAIL_ALREADY_EXISTS");

            existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            existingUser.DisplayName = request.DisplayName;
            existingUser.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(existingUser);
        }
        else
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                DisplayName = request.DisplayName,
                IsActive = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Users.AddAsync(newUser);
        }

        await _unitOfWork.SaveChangesAsync();

        var otpCode = new Random().Next(100000, 999999).ToString();
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync($"OTP:{request.Email}", otpCode, cacheOptions);

        var mailSent = await _emailService.SendOtpEmailAsync(request.Email, otpCode);
        if (!mailSent)
            throw new InvalidOperationException("EMAIL_SEND_FAILED");

        return true;
    }

    public async Task<AuthResponse?> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var savedOtp = await _cache.GetStringAsync($"OTP:{request.Email}");
        if (savedOtp == null || savedOtp != request.OtpCode) return null;

        var user = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);
        if (user == null) return null;

        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _cache.RemoveAsync($"OTP:{request.Email}");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email,
            DisplayName = user.DisplayName
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);
        if (user == null) return null;

        if (!user.IsActive || !user.IsEmailVerified) return null;

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email,
            DisplayName = user.DisplayName
        };
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);
        if (user == null || !user.IsActive) return false;

        var resetToken = Guid.NewGuid().ToString();
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };
        await _cache.SetStringAsync($"RESET:{resetToken}", request.Email, cacheOptions);

        // Link đặt lại mật khẩu trỏ về cổng chạy BE phục vụ việc test trực tiếp (ví dụ cổng 5070)
        var resetLink = $"https://localhost:7098/api/auth/reset-password-form?token={resetToken}";

        return await _emailService.SendPasswordResetLinkAsync(request.Email, resetLink);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = await _cache.GetStringAsync($"RESET:{request.Token}");
        if (string.IsNullOrEmpty(email)) return false;

        var user = await _unitOfWork.Users.FindAsync(u => u.Email == email);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _cache.RemoveAsync($"RESET:{request.Token}");
        return true;
    }
}
