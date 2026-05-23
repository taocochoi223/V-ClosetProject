using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs;

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google ID Token không được để trống.")]
    public string IdToken { get; set; } = null!;
}
