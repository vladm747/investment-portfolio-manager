using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PortfolioManager.API.Filters;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.MathModule.Client;

namespace PortfolioManager.Tests.Services;

public class ExceptionFilterTests
{
    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class RecordingLogger : ILogger<PortfolioManagerExceptionFilterAttribute>
    {
        public List<LogLevel> Levels { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }

    private static ExceptionContext Run(Exception exception, string environment = "Production",
        CancellationToken requestAborted = default, ILogger<PortfolioManagerExceptionFilterAttribute>? logger = null)
    {
        var filter = new PortfolioManagerExceptionFilterAttribute(
            new TestEnvironment(environment), logger ?? NullLogger<PortfolioManagerExceptionFilterAttribute>.Instance);
        var http = new DefaultHttpContext { RequestAborted = requestAborted };
        var context = new ExceptionContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>())
        {
            Exception = exception
        };

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        return context;
    }

    private static ObjectResult Handle(Exception exception, string environment = "Production") =>
        Assert.IsType<ObjectResult>(Run(exception, environment).Result);

    public static TheoryData<Exception, int> Mapping => new()
    {
        { new SymbolNotFoundException(), 400 },
        { new MathModuleRejectedException("not_enough_history", "too short"), 400 },
        { new MarketDataUnavailableException(), 502 },
        { new ValidationFailedException(new[] { "bad" }), 400 },
        { new AuthenticationFailedException("no"), 401 },
        { new NotAllowedException(), 403 },
        { NotFoundException.Portfolio(), 404 },
        { NotFoundException.Position(), 404 },
        { new UserNotFoundException(), 404 },
        { new InvalidOperationException("bug"), 500 },
        // A dictionary lookup that failed is a bug, not a missing record.
        { new KeyNotFoundException(), 500 },
    };

    [Theory]
    [MemberData(nameof(Mapping))]
    public void Each_exception_becomes_its_status_code(Exception exception, int expectedStatus) =>
        Assert.Equal(expectedStatus, Handle(exception).StatusCode);

    [Fact]
    public void An_unknown_ticker_tells_the_user_in_Ukrainian()
    {
        var error = Assert.IsType<ErrorDTO>(Handle(new SymbolNotFoundException()).Value);

        Assert.Equal("Такий тікер не знайдено", error.Message);
    }

    [Fact]
    public void An_unreachable_market_data_service_hides_its_details_in_production()
    {
        var error = Assert.IsType<ErrorDTO>(Handle(new MarketDataUnavailableException()).Value);

        Assert.Equal(MarketDataUnavailableException.DefaultMessage, error.Message);
        Assert.Null(error.StackTrace);
    }

    [Fact]
    public void A_not_found_message_is_the_same_for_a_missing_and_a_foreign_record()
    {
        var error = Assert.IsType<ErrorDTO>(Handle(NotFoundException.Portfolio()).Value);

        Assert.Equal("Портфель не знайдено.", error.Message);
    }

    // ---- unexpected failures ----------------------------------------------------------------------------------

    [Fact]
    public void An_unexpected_failure_does_not_show_its_message_in_production()
    {
        var error = Assert.IsType<ErrorDTO>(
            Handle(new InvalidOperationException("Cannot open database \"PortfolioManager\" on server tcp:secret.database.windows.net")).Value);

        Assert.Equal(PortfolioManagerExceptionFilterAttribute.InternalErrorMessage, error.Message);
        Assert.Null(error.StackTrace);
        Assert.Null(error.Source);
    }

    [Fact]
    public void An_unexpected_failure_shows_its_message_and_stack_trace_while_developing()
    {
        Exception thrown;
        try
        {
            throw new InvalidOperationException("the details"); // thrown, so that it has a stack trace
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        var error = Assert.IsType<ErrorDTO>(Handle(thrown, "Development").Value);

        Assert.Equal("the details", error.Message);
        Assert.NotNull(error.StackTrace);
    }

    [Fact]
    public void An_unexpected_failure_is_logged_as_an_error_but_an_expected_one_is_not()
    {
        var logger = new RecordingLogger();

        Run(NotFoundException.Position(), logger: logger);
        Run(new MarketDataUnavailableException(), logger: logger);
        Assert.Empty(logger.Levels);

        Run(new InvalidOperationException("bug"), logger: logger);
        Assert.Equal(new[] { LogLevel.Error }, logger.Levels);
    }

    // ---- cancellation -----------------------------------------------------------------------------------------

    [Fact]
    public void A_request_the_client_gave_up_on_is_not_an_error()
    {
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        var logger = new RecordingLogger();

        var context = Run(new OperationCanceledException(aborted.Token), requestAborted: aborted.Token, logger: logger);

        Assert.Equal(499, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public void A_cancellation_that_did_not_come_from_the_client_is_still_a_failure()
    {
        // For example a timeout inside the server: the client is still waiting for an answer.
        var context = Run(new OperationCanceledException(), requestAborted: CancellationToken.None);

        Assert.Equal(500, Assert.IsType<ObjectResult>(context.Result).StatusCode);
    }
}
