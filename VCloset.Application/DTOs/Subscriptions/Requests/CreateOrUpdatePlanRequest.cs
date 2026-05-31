using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Subscriptions.Requests;

public class CreateOrUpdatePlanRequest
{
    [Required(ErrorMessage = "Tên gói là bắt buộc")]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Giá tiền phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    [Required]
    public string Currency { get; set; } = "VND";

    [Required]
    [Range(1, 3650, ErrorMessage = "Số ngày hiệu lực phải từ 1 đến 3650")]
    public int DurationDays { get; set; }

    public bool IsActive { get; set; } = true;
}
