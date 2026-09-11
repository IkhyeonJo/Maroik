using Maroik.Website.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maroik.Website.Tests.Filters;

/// <summary>
/// Unit tests for <see cref="ThrottleEmailSendAttribute"/>: the per-address cooldown gate on the
/// account e-mail-sending POST actions. Covers the address-extraction fallback (bare string vs. a
/// bound view model's <c>Email</c> property), the cooldown-hit redirect (which must never reach the
/// action), the fail-open behaviour when the distributed cache is unavailable or unregistered, and
/// that the cooldown is only armed after an action that actually ran and passed model validation.
/// </summary>
public class ThrottleEmailSendAttributeTests
{
    private static (ActionExecutingContext context, FakeController controller) BuildContext(
        object? boundArgument, IDistributedCache? cache, string action = "ForgotPassword", string controllerName = "Account")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (cache != null)
            services.AddSingleton(cache);
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var controller = new FakeController
        {
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
        var actionDescriptor = new ControllerActionDescriptor
        {
            RouteValues = new Dictionary<string, string?> { ["controller"] = controllerName, ["action"] = action },
        };
        // ThrottleEmailSendAttribute reads context.RouteData.Values (not ActionDescriptor.RouteValues)
        // for the action/controller name, so both must carry the same values here.
        var routeData = new RouteData();
        routeData.Values["controller"] = controllerName;
        routeData.Values["action"] = action;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = routeData,
            ActionDescriptor = actionDescriptor,
        };

        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
        var actionArguments = new Dictionary<string, object?>();
        if (boundArgument != null)
            actionArguments["model"] = boundArgument;

        var executingContext = new ActionExecutingContext(actionContext, [], actionArguments, controller);
        return (executingContext, controller);
    }

    private static ActionExecutionDelegate NextReturning(Action onNext, Exception? exception = null) => () =>
    {
        onNext();
        return Task.FromResult(new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()), [], null!)
        {
            ExceptionHandled = exception == null,
            Exception = exception,
        });
    };

    private sealed class ModelWithEmail
    {
        public string? Email { get; set; }
    }

    // -- No throttling possible --------------------------------------------------

    /// <summary>No bound argument looks like an e-mail address: the action runs and the cache is never touched.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_NoEmailInArguments_RunsAction_NeverTouchesCache()
    {
        var cache = new Mock<IDistributedCache>();
        var (context, _) = BuildContext(boundArgument: "not-an-email", cache.Object);
        bool nextCalled = false;

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => nextCalled = true));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
        cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>No <see cref="IDistributedCache"/> registered in DI (e.g. Valkey misconfigured): the action still runs, unthrottled.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_NoCacheRegistered_RunsActionUnthrottled()
    {
        var (context, _) = BuildContext(boundArgument: "user@example.com", cache: null);
        bool nextCalled = false;

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => nextCalled = true));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    // -- Cooldown hit --------------------------------------------------------------

    /// <summary>An address still on cooldown is redirected back to the same action without ever running it.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_AddressOnCooldown_RedirectsWithoutRunningAction()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([1]);
        var (context, controller) = BuildContext(boundArgument: "user@example.com", cache.Object);
        bool nextCalled = false;

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => nextCalled = true));

        Assert.False(nextCalled);
        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("ForgotPassword", redirect.ActionName);
        Assert.Equal("Please wait a moment before requesting another email.", controller.TempData["Error"]);
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>The cooldown key is namespaced by action, lower-cased, and trimmed -- so casing/whitespace on the submitted address can't dodge or split the cooldown.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_BuildsCooldownKey_LowercasedTrimmed_NamespacedByAction()
    {
        var cache = new Mock<IDistributedCache>();
        string? capturedKey = null;
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKey = key)
            .ReturnsAsync((byte[]?)null);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var (context, _) = BuildContext(boundArgument: "  User@Example.COM  ", cache.Object, action: "Register");

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => { }));

        Assert.Equal("emailsend:Register:user@example.com", capturedKey);
    }

    // -- Address extraction fallback ------------------------------------------------

    /// <summary>The address is also found via reflection on a bound view model's <c>Email</c> property, not just a bare string argument.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_ExtractsEmail_FromViewModelProperty()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var (context, _) = BuildContext(boundArgument: new ModelWithEmail { Email = "model@example.com" }, cache.Object);

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => { }));

        cache.Verify(c => c.SetAsync(
            It.Is<string>(k => k.Contains("model@example.com", StringComparison.Ordinal)),
            It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- Fail-open on cache errors -----------------------------------------------

    /// <summary>A cache read failure (e.g. Valkey blip) fails open: the action still runs, unthrottled.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_CacheReadThrows_FailsOpen_RunsAction()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException("cache down"));
        var (context, _) = BuildContext(boundArgument: "user@example.com", cache.Object);
        bool nextCalled = false;

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => nextCalled = true));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A cache write failure after a successful send is swallowed -- the response the user already saw is unaffected.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_CacheWriteThrows_IsSwallowed()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("cache down"));
        var (context, _) = BuildContext(boundArgument: "user@example.com", cache.Object);
        bool nextCalled = false;

        var exception = await Record.ExceptionAsync(() =>
            new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => nextCalled = true)));

        Assert.Null(exception);
        Assert.True(nextCalled);
    }

    // -- Cooldown only armed after a real, successful send attempt ------------------

    /// <summary>After a well-formed address is processed successfully, the cooldown is armed with the configured window.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SuccessfulSend_ArmsCooldown_WithConfiguredWindow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        DistributedCacheEntryOptions? capturedOptions = null;
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, _, opts, _) => capturedOptions = opts)
            .Returns(Task.CompletedTask);
        var (context, _) = BuildContext(boundArgument: "user@example.com", cache.Object);

        var sut = new ThrottleEmailSendAttribute { WindowSeconds = 30 };
        await sut.OnActionExecutionAsync(context, NextReturning(() => { }));

        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(TimeSpan.FromSeconds(30), capturedOptions!.AbsoluteExpirationRelativeToNow);
    }

    /// <summary>An action that bounced on model validation must not consume the cooldown window -- the user can retry immediately.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_ModelStateInvalidAfterAction_DoesNotArmCooldown()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        var (context, _) = BuildContext(boundArgument: "user@example.com", cache.Object);
        context.ModelState.AddModelError("Nickname", "Nickname is required.");

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(context, NextReturning(() => { }));

        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>An action that threw does not consume the cooldown window either.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_ActionThrew_DoesNotArmCooldown()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        var (context, _) = BuildContext(boundArgument: "user@example.com", cache.Object);

        await new ThrottleEmailSendAttribute().OnActionExecutionAsync(
            context, NextReturning(() => { }, exception: new InvalidOperationException("boom")));

        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
