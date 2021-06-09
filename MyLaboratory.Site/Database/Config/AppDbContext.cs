using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Database.Tables;

namespace MyLaboratory.Site.Database.Config
{
     public class AppDbContext : DbContext
     {
        #region (For Migration) DbSet<T>'s T is table type.
        public DbSet<Product> Product { get; set; }
        public DbSet<Worker> Worker { get; set; }
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