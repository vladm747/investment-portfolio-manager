using Microsoft.AspNetCore.Identity;

namespace PortfolioManager.BLL.Services.Auth;

/// <summary>Identity's sign-up errors in Ukrainian, so the client can show them as they are.</summary>
public class UkrainianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Користувач з поштою {userName} вже зареєстрований.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"Користувач з поштою {email} вже зареєстрований.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), "Пошта може містити лише латинські літери, цифри та символи @ . - _ +");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), "Некоректна адреса пошти.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Пароль має містити щонайменше {length} символів.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Пароль має містити хоча б один спецсимвол (наприклад, ! або #).");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Пароль має містити хоча б одну цифру.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Пароль має містити хоча б одну малу латинську літеру.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Пароль має містити хоча б одну велику латинську літеру.");

    private static IdentityError Error(string code, string description) =>
        new() { Code = code, Description = description };
}
