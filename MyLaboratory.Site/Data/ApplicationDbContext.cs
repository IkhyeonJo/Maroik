using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Database.Tables;

namespace MyLaboratory.Site.Data
{
     public class ApplicationDbContext : DbContext
     {
        #region (For Migration) DbSet<T>'s T is table type.
        public DbSet<Product> Product { get; set; }
        public DbSet<Worker> Worker { get; set; }
        #endregion
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
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