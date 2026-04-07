using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Core.Repository.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddRepositoryContext(IConfiguration configuration)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(0)
                ).EnableSensitiveDataLogging()
            );
        }

        public void AddRepositoryServices()
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<IIncomeRepository, IncomeRepository>();
            services.AddScoped<IExpenditureRepository, ExpenditureRepository>();
            services.AddScoped<IFixedIncomeRepository, FixedIncomeRepository>();
            services.AddScoped<IFixedExpenditureRepository, FixedExpenditureRepository>();
            services.AddScoped<IBoardRepository, BoardRepository>();
            services.AddScoped<IBoardAttachedFileRepository, BoardAttachedFileRepository>();
            services.AddScoped<IBoardCommentRepository, BoardCommentRepository>();
            services.AddScoped<ICalendarRepository, CalendarRepository>();
            services.AddScoped<IOtherCalendarRepository, OtherCalendarRepository>();
            services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
            services.AddScoped<ICalendarEventAttachedFileRepository, CalendarEventAttachedFileRepository>();
            services.AddScoped<ICalendarEventReminderRepository, CalendarEventReminderRepository>();
            services.AddScoped<ICalendarSharedRepository, CalendarSharedRepository>();
        }
    }
}
