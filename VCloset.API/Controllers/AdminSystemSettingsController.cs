using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

/// <summary>
/// Quản lý cấu hình hệ thống (Admin).
/// </summary>
[Route("api/admin/system-settings")]
[ApiController]
[Authorize]
[RequirePermission("subscription.manage")]
public class AdminSystemSettingsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminSystemSettingsController> _logger;

    public AdminSystemSettingsController(IUnitOfWork unitOfWork, ILogger<AdminSystemSettingsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Cập nhật liên kết khảo sát hệ thống (survey_url).
    /// </summary>
    [HttpPut("survey-url")]
    public async Task<IActionResult> UpdateSurveyUrl([FromBody] UpdateSurveyUrlRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SurveyUrl))
            {
                return BadRequest(new { message = "Đường dẫn URL khảo sát không được để trống." });
            }

            var surveyUrl = request.SurveyUrl.Trim();
            if (!surveyUrl.StartsWith("http://") && !surveyUrl.StartsWith("https://"))
            {
                return BadRequest(new { message = "Đường dẫn phải bắt đầu bằng http:// hoặc https://" });
            }

            var setting = await _unitOfWork.SystemSettings.FindAsync(s => s.SettingKey == "survey_url");
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    SettingKey = "survey_url",
                    SettingValue = surveyUrl
                };
                await _unitOfWork.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.SettingValue = surveyUrl;
                _unitOfWork.SystemSettings.Update(setting);
            }

            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Admin updated system survey URL to: {SurveyUrl}", surveyUrl);
            return Ok(new { message = "Đã cập nhật liên kết khảo sát thành công!", surveyUrl = surveyUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật liên kết khảo sát hệ thống.");
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdateSurveyUrlRequest
{
    public string SurveyUrl { get; set; } = null!;
}
