using System;

namespace VCloset.Application.DTOs;

public class UpdateProfileRequest
{
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }
}
