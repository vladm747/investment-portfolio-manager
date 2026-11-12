using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.API.Controllers;

namespace PortfolioManager.Tests.Api;

/// <summary>
/// Rules every controller must follow, checked on all of them so a controller added later cannot forget one.
/// </summary>
public class ControllerConventionTests
{
    private static readonly Type[] Controllers = typeof(PortfolioController).Assembly.GetTypes()
        .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
        .ToArray();

    /// <summary>The controllers that serve a user's own data. Their user comes from the token.</summary>
    private static readonly Type[] DataControllers =
        { typeof(PortfolioController), typeof(PositionController), typeof(PortfolioStatisticController) };

    private static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes().Any(attribute => attribute is Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute));

    [Fact]
    public void There_are_controllers_to_check()
    {
        Assert.True(Controllers.Length >= 5, "Found " + string.Join(", ", Controllers.Select(type => type.Name)));
    }

    [Fact]
    public void Every_controller_is_an_ApiController_so_invalid_requests_are_refused_with_a_400()
    {
        var without = Controllers.Where(type => type.GetCustomAttribute<ApiControllerAttribute>() is null).Select(type => type.Name);

        Assert.Empty(without);
    }

    [Fact]
    public void Every_controller_says_who_may_call_it()
    {
        var silent = Controllers.Where(type =>
            type.GetCustomAttribute<AuthorizeAttribute>() is null && type.GetCustomAttribute<AllowAnonymousAttribute>() is null)
            .Select(type => type.Name);

        Assert.Empty(silent);
    }

    [Fact]
    public void The_data_controllers_require_a_signed_in_user_and_never_allow_anonymous_actions()
    {
        foreach (var controller in DataControllers)
        {
            Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            Assert.All(Actions(controller), action => Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>()));
        }
    }

    [Fact]
    public void Every_async_action_of_the_data_controllers_passes_the_request_cancellation_token_on()
    {
        var missing = DataControllers
            .SelectMany(Actions)
            .Where(action => typeof(Task).IsAssignableFrom(action.ReturnType))
            .Where(action => action.GetParameters().All(parameter => parameter.ParameterType != typeof(CancellationToken)))
            .Select(action => action.DeclaringType!.Name + "." + action.Name);

        Assert.Empty(missing);
    }

    [Fact]
    public void No_action_of_the_data_controllers_takes_a_user_id_from_the_client()
    {
        // The owner is the user in the access token. An action with a user id parameter, or a request body
        // that carries one, would let the client pick whose data it touches.
        var offenders = DataControllers
            .SelectMany(Actions)
            .SelectMany(action => action.GetParameters().Select(parameter => (action, parameter)))
            .Where(pair => pair.parameter.Name!.Contains("userid", StringComparison.OrdinalIgnoreCase)
                           || pair.parameter.ParameterType.GetProperties().Any(property =>
                               property.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase)))
            .Select(pair => pair.action.DeclaringType!.Name + "." + pair.action.Name + "(" + pair.parameter.Name + ")");

        Assert.Empty(offenders);
    }

    [Fact]
    public void Listing_all_users_is_for_admins_only()
    {
        var action = typeof(UserController).GetMethod(nameof(UserController.GetAllUsersAsync))!;

        var authorize = Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("admin", authorize.Roles);
    }
}
