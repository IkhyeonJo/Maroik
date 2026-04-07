using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Core.Service.Extensions;

/// <summary>
/// Extension methods for registering Maroik.Core.Service implementations into the DI container.
/// Call <see cref="AddApplicationServices"/> from Program.cs during application startup.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application service implementations as scoped services,
    /// except <see cref="IRsaService"/> which is registered as a singleton
    /// because RSA key material is loaded once and reused across requests.
    /// </summary>
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IIncomeService, IncomeService>();
        services.AddScoped<IExpenditureService, ExpenditureService>();
        services.AddScoped<IFixedIncomeService, FixedIncomeService>();
        services.AddScoped<IFixedExpenditureService, FixedExpenditureService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IManagementAccountService, ManagementAccountService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddSingleton<IRsaService, RsaService>();
    }
}
