using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Auth.Requests;

namespace VCloset.Application.Interfaces;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> VerifyOtpAsync(VerifyOtpRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<AuthResponse?> GoogleLoginAsync(GoogleLoginRequest request);
    Task<bool> ResendOtpAsync(ResendOtpRequest request);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(int userId, string refreshToken);
}
