using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens.Experimental;
using System;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Auth.Requests;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

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
                UpdatedAt = DateTime.UtcNow,
                Role = UserRole.Customer,
                AuthProvider = AuthProvider.Local
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
        await _unitOfWork.SaveChangesAsync(); // Lúc này user.InternalId đã có giá trị thật

        await _cache.RemoveAsync($"OTP:{request.Email}");

        // Tạo profile theo role (nếu chưa có)
        await CreateProfileIfNotExistsAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.InternalId);

        var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == user.InternalId);

        var displayRole = await GetDisplayRoleAsync(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Role = displayRole,
            UserId = user.InternalId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            IsOnboardingCompleted = user.Role != UserRole.Customer || (profile?.IsOnboardingCompleted ?? false),
            IsPasswordSet = !string.IsNullOrEmpty(user.PasswordHash)
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users
            .FindAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Email hoặc mật khẩu không đúng");

        if (!user.IsEmailVerified)
            throw new Exception("Email chưa được kích hoạt.");

        if (!user.IsActive)
            throw new Exception("Tài khoản đã bị khoá.");

        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new Exception("Tài khoản này chưa được thiết lập mật khẩu.");;

        var accessToken = _jwtService.GenerateAccessToken(user);

        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.InternalId);

        var profile = await _unitOfWork.CustomerProfiles
            .FindAsync(c => c.UserInternalId == user.InternalId);
        var displayRole = await GetDisplayRoleAsync(user);
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Role = displayRole,
            UserId = user.InternalId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            IsOnboardingCompleted = user.Role != UserRole.Customer || (profile?.IsOnboardingCompleted ?? false),
            IsPasswordSet = !string.IsNullOrEmpty(user.PasswordHash)
        };
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);
        if (user == null || !user.IsActive) 
            return false;

        var otpCode = new Random().Next(100000, 999999).ToString();

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };
        await _cache.SetStringAsync($"RESET_OTP:{request.Email}", otpCode, cacheOptions);

        return await _emailService.SendForgotPasswordOtpEmailAsync(request.Email, otpCode);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var savedOtp = await _cache.GetStringAsync($"RESET_OTP:{request.Email}");
        if (string.IsNullOrEmpty(savedOtp) || savedOtp != request.OtpCode) 
            return false;

        var user = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);

        if (user == null) 
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _cache.RemoveAsync($"RESET_OTP:{request.Email}");
        return true;
    }

    public async Task<AuthResponse?> GoogleLoginAsync(GoogleLoginRequest request)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>()
                {
                    Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? ""
                }
            };

            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

            var user = await _unitOfWork.Users.FindAsync(u => u.Email == payload.Email || u.GoogleId == payload.Subject);

            if(user == null)
            {
                user = new User()
                {
                    Id = Guid.NewGuid(),
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    GoogleId = payload.Subject,
                    AvatarUrl = payload.Picture,
                    AuthProvider = AuthProvider.Google,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync(); // Lúc này user.InternalId đã có giá trị thật

                // Tạo profile theo role
                await CreateProfileIfNotExistsAsync(user);
            }
            else if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = payload.Subject;
                user.AuthProvider = AuthProvider.Google;

                if (string.IsNullOrEmpty(user.AvatarUrl))
                {
                    user.AvatarUrl = payload.Picture;
                }
                
                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }


            if (!user.IsActive) return null;

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.InternalId);

            var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == user.InternalId);

            var displayRole = await GetDisplayRoleAsync(user);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = displayRole,
                UserId = user.InternalId,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                IsOnboardingCompleted = user.Role != UserRole.Customer || (profile?.IsOnboardingCompleted ?? false),
                IsPasswordSet = !string.IsNullOrEmpty(user.PasswordHash)
            };
        }
        catch (InvalidJwtException ex)
        {
            Console.WriteLine($"\n[LỖI GOOGLE TOKEN]: {ex.Message}\n");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[LỖI CHUNG]: {ex.Message}\n");
            return null;
        }

    }

    public async Task<bool> ResendOtpAsync(ResendOtpRequest request)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);
        if (user == null) throw new Exception("Tài khoản không tồn tại");
        if (user.IsEmailVerified) throw new Exception("Tài khoản đã được kích hoạt");

        var otp = new Random().Next(100000, 999999).ToString();
        await _cache.SetStringAsync($"OTP:{request.Email}", otp, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        var emailSent = await _emailService.SendOtpEmailAsync(user.Email, otp);

        if (!emailSent) throw new InvalidOperationException("EMAIL_SEND_FAILED");
        return true;

    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new Exception("Tài khoản không tồn tại");
        
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (string.IsNullOrEmpty(request.OldPassword))
                throw new Exception("Mật khẩu cũ không được để trống.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash)) 
                throw new Exception("Mật khẩu cũ không đúng");
        }
        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userIdString = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            throw new Exception("Access Token không hợp lệ.");

        var tokenRecord = await _unitOfWork.RefreshTokens.FindAsync(t =>
            t.TokenHash == request.RefreshToken &&
            t.UserInternalId == userId);

        if (tokenRecord == null || tokenRecord.ExpiresAt <= DateTime.UtcNow || tokenRecord.RevokedAt != null)
            throw new Exception("Refresh Token không hợp lệ hoặc đã hết hạn.");

        _unitOfWork.RefreshTokens.Delete(tokenRecord);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new Exception("Không tìm thấy người dùng.");

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.InternalId);

        var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == user.InternalId);

        var displayRole = await GetDisplayRoleAsync(user);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            Role = displayRole,
            UserId = user.InternalId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            IsOnboardingCompleted = user.Role != UserRole.Customer || (profile?.IsOnboardingCompleted ?? false),
            IsPasswordSet = !string.IsNullOrEmpty(user.PasswordHash)
        };
    }

    public async Task<bool> LogoutAsync(int userId, string refreshToken)
    {
        
        var tokenRecord = await _unitOfWork.RefreshTokens.FindAsync(t =>
            t.TokenHash == refreshToken &&
            t.UserInternalId == userId);

        if (tokenRecord != null)
        {
            _unitOfWork.RefreshTokens.Delete(tokenRecord);
            await _unitOfWork.SaveChangesAsync();
        }
        return true;
    }



    private async Task<string> GenerateAndSaveRefreshTokenAsync(int userInternalId)
    {
        // Thu hồi (xóa) toàn bộ Refresh Token cũ của user này để ngăn đăng nhập đồng thời nhiều thiết bị
        var oldTokens = await _unitOfWork.RefreshTokens.FindAllAsync(t => t.UserInternalId == userInternalId);
        foreach (var token in oldTokens)
        {
            _unitOfWork.RefreshTokens.Delete(token);
        }

        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenEntity = new RefreshToken
        {
            UserInternalId = userInternalId,
            TokenHash = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30), 
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.RefreshTokens.AddAsync(tokenEntity);
        await _unitOfWork.SaveChangesAsync();
        return refreshToken;
    }

    private async Task CreateProfileIfNotExistsAsync(User user)
    {
        switch (user.Role)
        {
            case UserRole.Admin:
            case UserRole.Moderator:
            {
                var existing = await _unitOfWork.AdminProfiles.FindAsync(a => a.UserInternalId == user.InternalId);
                if (existing != null) return;

                var allLevels = await _unitOfWork.PermissionLevels.GetAllAsync();
                var lowestLevel = allLevels.OrderBy(l => l.Id).FirstOrDefault();

                var adminProfile = new AdminProfile
                {
                    Id = Guid.NewGuid(),
                    UserInternalId = user.InternalId,
                    PermissionLevel = (short)(lowestLevel?.Id ?? 1),
                    Department = null,
                    Notes = null,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AdminProfiles.AddAsync(adminProfile);
                await _unitOfWork.SaveChangesAsync();
                break;
            }

            case UserRole.BrandPartner:
            {
                var existing = await _unitOfWork.BrandProfiles.FindAsync(b => b.UserInternalId == user.InternalId);
                if (existing != null) return;

                var brandProfile = new BrandProfile
                {
                    Id = Guid.NewGuid(),
                    UserInternalId = user.InternalId,
                    BrandName = user.DisplayName,
                    LogoUrl = null,
                    WebsiteUrl = null,
                    ContactPhone = null,
                    TaxCode = null,
                    CreditBalance = 0,
                    Status = Domain.Enums.BrandStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.BrandProfiles.AddAsync(brandProfile);
                await _unitOfWork.SaveChangesAsync();
                break;
            }

            case UserRole.Customer:
            default:
            {
                var existing = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == user.InternalId);
                if (existing != null) return;

                var customerProfile = new CustomerProfile
                {
                    Id = Guid.NewGuid(),
                    UserInternalId = user.InternalId,
                    HeightCm = null,
                    WeightKg = null,
                    DateOfBirth = null,
                    PhoneNumber = null,
                    Address = null,
                    Gender = null,
                    Country = null,
                    MannequinImageUrl = null,
                    MannequinGeneratedAt = null,
                    WardrobeItemCount = 0,
                    IsChatBanned = false,
                    IsPostBanned = false,
                    ChatBannedUntil = null,
                    PostBannedUntil = null,
                    IsOnboardingCompleted = false,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.CustomerProfiles.AddAsync(customerProfile);
                await _unitOfWork.SaveChangesAsync();
                break;
            }
        }
    }

    private async Task<string> GetDisplayRoleAsync(User user)
    {
        if (user.Role == UserRole.Admin)
        {
            var adminProfile = await _unitOfWork.AdminProfiles.FindAsync(a => a.UserInternalId == user.InternalId);
            if (adminProfile?.PermissionLevel == 3)
            {
                return "SuperAdmin";
            }
        }
        return user.Role.ToString();
    }

}

