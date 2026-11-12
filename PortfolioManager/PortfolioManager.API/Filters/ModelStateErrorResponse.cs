using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PortfolioManager.BLL.Exceptions;

namespace PortfolioManager.API.Filters;

/// <summary>
/// The 400 that [ApiController] sends by itself when the request does not pass validation. It has the same body
/// as every other error of the API, <c>{ "message": ..., "errors": [...] }</c>, instead of ASP.NET's
/// ProblemDetails, so the client reads all errors in one way.
/// </summary>
public static class ModelStateErrorResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var messages = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => MessageOf(entry.Key, error)))
            .ToList();

        if (messages.Count == 0)
            messages.Add("Запит містить некоректні дані.");

        var exception = new ValidationFailedException(messages);

        return new BadRequestObjectResult(new ErrorDTO { Message = exception.Message, Errors = exception.Errors });
    }

    private static string MessageOf(string key, ModelError error)
    {
        // A validation attribute carries a message written for the user. A binding error (text where a number
        // should be, broken JSON) carries the parser's English text and the type names, which stay on the server.
        if (error.Exception is null && !string.IsNullOrWhiteSpace(error.ErrorMessage))
            return error.ErrorMessage;

        var field = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
        return string.IsNullOrWhiteSpace(field) || field == "$"
            ? "Запит має некоректний формат."
            : $"Некоректне значення поля «{field}».";
    }
}
