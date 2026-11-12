using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.Validation;

/// <summary>
/// A date that is not after today. The client sends the picked day as a date and time in its own time zone, so
/// the check compares calendar days and allows one day of slack: someone ahead of UTC on the evening of "today"
/// is already on tomorrow's date in UTC and must not be refused.
/// A missing date arrives as <see cref="DateTime.MinValue"/>, so a date before 1970 is refused too
/// (the price history the app can use starts long after that).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotInFutureAttribute : ValidationAttribute
{
    private static readonly DateTime Earliest = new(1970, 1, 1);

    public NotInFutureAttribute() : base("Дата не може бути в майбутньому.") { }

    /// <summary>Shown when the date is missing or before 1970.</summary>
    public string TooEarlyMessage { get; set; } = "Вкажіть коректну дату.";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime date)
            return ValidationResult.Success; // a missing value is for [Required] to judge

        var members = validationContext.MemberName is { } member ? new[] { member } : null;

        if (date.Date < Earliest)
            return new ValidationResult(TooEarlyMessage, members);

        var clock = validationContext.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        var latestAllowed = clock.GetUtcNow().UtcDateTime.Date.AddDays(1);

        return date.Date <= latestAllowed
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName), members);
    }
}
