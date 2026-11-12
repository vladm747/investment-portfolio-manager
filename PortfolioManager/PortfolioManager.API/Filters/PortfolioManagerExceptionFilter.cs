using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.MathModule.Client;

namespace PortfolioManager.API.Filters;

public class PortfolioManagerExceptionFilterAttribute : Attribute, IExceptionFilter
{
    /// <summary>What an unexpected failure tells the client. The real message may name tables, paths or hosts.</summary>
    public const string InternalErrorMessage = "Сталася внутрішня помилка. Спробуйте пізніше.";

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

        // The client closed the connection or navigated away while the request was running. Nobody is left to read
        // an answer and nothing went wrong, so it is neither logged as an error nor turned into a 500.
        if (ex is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
            context.ExceptionHandled = true;
            return;
        }

        var statusCode = ex switch
        {
            ValidationFailedException or SymbolNotFoundException or MathModuleRejectedException
                => StatusCodes.Status400BadRequest,
            AuthenticationFailedException => StatusCodes.Status401Unauthorized,
            NotAllowedException => StatusCodes.Status403Forbidden,
            // Only the exceptions we throw on purpose are a 404. A stray KeyNotFoundException from a dictionary
            // lookup is a bug and stays a 500.
            NotFoundException or UserNotFoundException => StatusCodes.Status404NotFound,
            // The math module (and Yahoo Finance behind it) is an upstream service: bad gateway, not our bug.
            MarketDataUnavailableException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        var isUnexpected = statusCode == StatusCodes.Status500InternalServerError;
        if (isUnexpected)
            _logger.LogError(ex, "Unhandled exception in {Action}", context.ActionDescriptor.DisplayName);

        var error = new ErrorDTO
        {
            // The messages of the exceptions above are written for the user; the message of anything else is not.
            Message = isUnexpected && !_environment.IsDevelopment() ? InternalErrorMessage : ex.Message,
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
