using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AspNetCoreMVCSample.src.Site.Models
{
     public class AppDbContext : DbContext
     {
        public DbSet<Produto> Produtos { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        // public AppDbContext()
        // {
        //     // Database.EnsureCreated();
        // }

         protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    base.OnModelCreating(modelBuilder);
                }

                protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                {
                    base.OnConfiguring(optionsBuilder);
                    // get the configuration from the app settings
                    // var config = new ConfigurationBuilder()
                    //     .SetBasePath(Directory.GetCurrentDirectory())
                    //     .AddJsonFile("appsettings.json")
                    //     .Build();
                    
                    // optionsBuilder.UseMySql(config["ConnectionStrings:connStrMySql"]);   

                    // var host = config["DBHOST"] ?? "localhost";
                    // var port = config["DBPORT"] ?? "3306";
                    // var password = config["DBPASSWORD"] ?? "root";
            
                    // optionsBuilder.UseMySql($"server={host};port={port};userid=root;pwd={password};database=produtosdb", providerOptions => providerOptions.EnableRetryOnFailure());         
                    // optionsBuilder.UseMySql(config["mysqlconnection:connectionString"], providerOptions => providerOptions.EnableRetryOnFailure());         
                }
    }
}