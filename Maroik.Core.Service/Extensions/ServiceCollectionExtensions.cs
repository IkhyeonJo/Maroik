using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Core.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetBalanceDomainService, AssetBalanceDomainService>();
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
