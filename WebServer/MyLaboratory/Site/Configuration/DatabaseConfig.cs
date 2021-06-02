using System;
using AspNetCoreMVCSample.src.Site.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreMVCSample.src.Site.Configuration
{
    public static class DatabaseConfig
    {
        public static void AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            //services.AddDbContext<AppDbContext>(options =>
            //    options.UseMySql(configuration["mysqlconnection:connectionString"],  providerOptions => providerOptions.EnableRetryOnFailure()));
            services.AddDbContext<AppDbContext>(options =>
                  options.UseMySql(configuration.GetConnectionString("DefaultConnection"), new MariaDbServerVersion(new Version(5, 7, 31))));
        }
    }
}