using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Admin.Requests;

public class UpdateAdminInternalInfoRequest
{
    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
