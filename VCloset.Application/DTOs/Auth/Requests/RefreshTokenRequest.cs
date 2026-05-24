using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string AccessToken { get; set; } = null!;

    [Required]
    public string RefreshToken { get; set; } = null!;
}

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}