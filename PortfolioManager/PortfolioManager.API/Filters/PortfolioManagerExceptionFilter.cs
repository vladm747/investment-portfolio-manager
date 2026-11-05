using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PortfolioManager.BLL.Exceptions;

namespace PortfolioManager.API.Filters;

public class PortfolioManagerExceptionFilterAttribute : Attribute, IExceptionFilter
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PortfolioManagerExceptionFilterAttribute> _logger;

    public PortfolioManagerExceptionFilterAttribute(IHostEnvironment environment,
        ILogger<PortfolioManagerExceptionFilterAttribute> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        var statusCode = ex switch
        {
            ValidationFailedException => StatusCodes.Status400BadRequest,
            AuthenticationFailedException => StatusCodes.Status401Unauthorized,
            NotAllowedException => StatusCodes.Status403Forbidden,
            UserNotFoundException or KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception in {Action}", context.ActionDescriptor.DisplayName);

        var error = new ErrorDTO
        {
            Message = ex.Message,
            Errors = (ex as ValidationFailedException)?.Errors
        };

        // Stack traces help while developing but tell an attacker how the server is built.
        if (_environment.IsDevelopment())
        {
            error.StackTrace = ex.StackTrace;
            error.Source = ex.Source;
        }

        context.Result = new ObjectResult(error) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
