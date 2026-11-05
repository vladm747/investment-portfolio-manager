using System.Text.Json.Serialization;

namespace PortfolioManager.API.Filters;

public class ErrorDTO
{
    public string Message { get; set; } = string.Empty;

    /// <summary>Individual validation messages, for example each failed password rule.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Errors { get; set; }

    /// <summary>Only filled in the Development environment.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; set; }

    /// <summary>Only filled in the Development environment.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; set; }
}
