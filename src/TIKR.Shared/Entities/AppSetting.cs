namespace TIKR.Shared.Entities;

/// <summary>Clerk-editable runtime setting (overrides env/appsettings without restart for most AI flags).</summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
