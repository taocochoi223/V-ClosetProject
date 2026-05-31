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

    [Range(1, 3650, ErrorMessage = "Số ngày hiệu lực phải từ 1 đến 3650")]
    public int? DurationDays { get; set; }

    public bool? IsActive { get; set; }
}
