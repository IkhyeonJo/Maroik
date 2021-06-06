using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Site.Database.Repositories.Interface;
using Site.Database.Repositories.Class;

namespace Site.Database.Config
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMariaDbContext(this IServiceCollection services)
        {
            string connectionString = "server=mariadb;port=3306;userid=root;password=root;database=ikhyeondb;"; // publish
            //string connectionString = "server=localhost;port=3306;userid=root;password=root;database=ikhyeondb;"; // debugging
            services.AddDbContext<AppDbContext>(options =>
                  options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            return services;
        }
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IWorkerRepository, WorkerRepository>();
            return services;
        }
    }
}
