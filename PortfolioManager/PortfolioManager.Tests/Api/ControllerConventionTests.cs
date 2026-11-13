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
    public void The_admin_controller_is_for_admins_only_and_no_action_in_it_opens_up_again()
    {
        var authorize = Assert.Single(typeof(AdminController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("admin", authorize.Roles);
        Assert.Null(typeof(AdminController).GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotEmpty(Actions(typeof(AdminController)));
        Assert.All(Actions(typeof(AdminController)), action =>
        {
            Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
            // The class-level rule is the whole rule: an action-level [Authorize] could only be there to change it.
            Assert.Empty(action.GetCustomAttributes<AuthorizeAttribute>());
        });
    }

    [Fact]
    public void Only_the_auth_controller_is_open_to_anonymous_callers_and_the_user_controller_lists_nobody()
    {
        var anonymous = Controllers
            .Where(type => type.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(type => type.Name);
        Assert.Equal(new[] { nameof(AuthController) }, anonymous);

        // Everybody's profile belongs to the admin controller; the user controller only knows the caller.
        var userActions = Actions(typeof(UserController)).Select(action => action.Name).OrderBy(name => name);
        Assert.Equal(new[] { nameof(UserController.DeleteAccount), nameof(UserController.GetCurrentUser) }, userActions);
    }
}
