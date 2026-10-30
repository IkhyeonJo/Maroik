using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Accounts;
using Maroik.Website.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Every admin write on the Management page (account create / update / delete, menu category and
/// sub-category create / update / delete) hands the signed-in administrator's e-mail to the service as
/// the actor, so the audit log records who made the change. The services are replaced by recording fakes
/// in one derived host; the admin session is real.
/// </summary>
[Collection("Website Integration")]
public class ManagementControllerAuditActorTests(MaroikWebApplicationFactory factory)
{
    /// <summary>The admin who performs every write; the services must receive this e-mail as the actor.</summary>
    private const string AdminEmail = "audit-actor-admin@test.com";

    /// <summary>A valid admin create/update-account JSON body.</summary>
    private static object AccountPayload() => new
    {
        Email = "audit-actor-target@test.com",
        Password = "NewAccountPw1!",
        Nickname = "AuditActorTarget",
        AvatarImagePath = "",
        Role = Role.User,
        TimeZoneIanaId = "UTC",
        Locked = false,
        LoginAttempt = 0,
        EmailConfirmed = true,
        AgreedServiceTerms = true,
        RegistrationToken = "",
        ResetPasswordToken = "",
        Message = "",
        Deleted = false
    };

    /// <summary>A valid menu JSON body, usable for both category and sub-category writes.</summary>
    private static object MenuPayload() => new
    {
        Id = 1,
        CategoryId = 1,
        Name = "AuditActorMenu",
        DisplayName = "AuditActorMenu",
        IconPath = "/icons/test.png",
        Controller = "Notice",
        Action = "Index",
        Role = Role.User,
        Order = 500
    };

    /// <summary>Each admin write endpoint passes the signed-in admin's e-mail to its service as the actor.</summary>
    [Fact]
    public async Task AdminWrites_PassTheSignedInAdminAsTheActor()
    {
        ServiceResult ok = ServiceResult.Ok();
        var accounts = new FakeManagementAccountService
        {
            OnCreateAccountAsync = (_, _, _) => Task.FromResult(ok),
            OnUpdateAccountAsync = (_, _, _, _) => Task.FromResult(ok),
            OnDeleteAccountAsync = (_, _, _) => Task.FromResult(ok),
        };
        var menus = new FakeMenuService
        {
            OnGetAllCategoriesAsync = _ => Task.FromResult<IEnumerable<CategoryResponse>>(
            [
                new CategoryResponse { Id = 1, Name = "Management", DisplayName = "Management", Controller = "Management", Action = "Account", Role = Role.Admin }
            ]),
            OnCreateCategoryAsync = (_, _, _) => Task.FromResult(ok),
            OnUpdateCategoryAsync = (_, _, _) => Task.FromResult(ok),
            OnDeleteCategoryAsync = (_, _, _) => Task.FromResult(ok),
            OnCreateSubCategoryAsync = (_, _, _) => Task.FromResult(ok),
            OnUpdateSubCategoryAsync = (_, _, _) => Task.FromResult(ok),
            OnDeleteSubCategoryAsync = (_, _, _) => Task.FromResult(ok),
        };

        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IManagementAccountService>();
            services.RemoveAll<IMenuService>();
            services.AddSingleton<IManagementAccountService>(accounts);
            services.AddSingleton<IMenuService>(menus);
        }));
        var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://www.localhost/"), AllowAutoRedirect = false, HandleCookies = false
        });
        var admin = await AuthenticatedSessionHelper.LoginAsync(host, client, AdminEmail, "AdminPassword1!", Role.Admin, TestContext.Current.CancellationToken);

        (string Url, object Payload)[] writes =
        [
            ("/Management/CreateAccount", AccountPayload()),
            ("/Management/UpdateAccount", AccountPayload()),
            ("/Management/DeleteAccount", AccountPayload()),
            ("/Management/CreateCategory", MenuPayload()),
            ("/Management/UpdateCategory", MenuPayload()),
            ("/Management/DeleteCategory", MenuPayload()),
            ("/Management/CreateSubCategory", MenuPayload()),
            ("/Management/UpdateSubCategory", MenuPayload()),
            ("/Management/DeleteSubCategory", MenuPayload()),
        ];
        foreach (var (url, payload) in writes)
        {
            using var request = admin.BuildJsonPostRequest(url, payload);
            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(json.Contains("\"result\":true", StringComparison.Ordinal), $"{url}: {(int)response.StatusCode} {json}");
        }

        Assert.Equal([AdminEmail], accounts.CreateAccountAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], accounts.UpdateAccountAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], accounts.DeleteAccountAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], menus.CreateCategoryAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], menus.UpdateCategoryAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], menus.DeleteCategoryAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], menus.CreateSubCategoryAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], menus.UpdateSubCategoryAsyncCalls.Select(c => c.ActorEmail));
        Assert.Equal([AdminEmail], menus.DeleteSubCategoryAsyncCalls.Select(c => c.ActorEmail));
    }
}
