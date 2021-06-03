using Microsoft.EntityFrameworkCore;

namespace AspNetCoreMVCSample.src.Site.Models.Home
{
     public class AppDbContext : DbContext
     {
        #region (For Migration) DbSet<T>'s T is table type.
        public DbSet<Product> Product { get; set; }
        #endregion
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}