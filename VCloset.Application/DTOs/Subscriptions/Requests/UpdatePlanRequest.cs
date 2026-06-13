using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Subscriptions.Requests;

public class UpdatePlanRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá tiền phải lớn hơn hoặc bằng 0")]
    public decimal? Price { get; set; }

    public string? Currency { get; set; }

    public int? DurationDays { get; set; }

    [Range(0, 10000, ErrorMessage = "Số lượt xóa nền không hợp lệ")]
    public int GrantedBgCredits { get; set; } = 0;

    [Range(0, 10000, ErrorMessage = "Số lượt thử đồ không hợp lệ")]
    public int GrantedTryOnCredits { get; set; } = 0;

    public bool? IsActive { get; set; }
}
