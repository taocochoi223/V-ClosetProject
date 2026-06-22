using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCloset.Domain.Entities;

[Table("system_settings")]
public class SystemSetting
{
    [Key]
    [Column("setting_key")]
    [MaxLength(100)]
    public string SettingKey { get; set; } = null!;

    [Column("setting_value")]
    public string SettingValue { get; set; } = null!;
}
