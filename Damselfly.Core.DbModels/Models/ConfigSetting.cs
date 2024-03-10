using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Core.Models;

/// <summary>
///     Config associated with an export or download
/// </summary>
public class ConfigSetting
{
    public int ConfigSettingId { get; set; }
    public required string Name { get; set; }
    public string? Value { get; set; }

    public int? UserId { get; set; }
    public virtual AppIdentityUser User { get; set; }

    public override string ToString()
    {
        var scope = UserId == null ? "Global" : "User";
        return $"Setting: {Name} = {Value} ({scope})";
    }
}