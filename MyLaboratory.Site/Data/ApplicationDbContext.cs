using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Models;

namespace MyLaboratory.Site.Data
{
     public partial class ApplicationDbContext : DbContext
     {
        public ApplicationDbContext()
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        #region (For Migration) DbSet<T>'s T is table type.
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<SubCategory> SubCategories { get; set; }
        public virtual DbSet<Worker> Workers { get; set; }
        #endregion
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet("utf8mb4")
    .UseCollation("utf8mb4_general_ci");

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Email)
                    .HasName("PRIMARY");

                entity.ToTable("Account");

                entity.Property(e => e.FirstName).HasMaxLength(256);

                entity.Property(e => e.HashedPassword).IsRequired();

                entity.Property(e => e.LastName).HasMaxLength(256);

                entity.Property(e => e.Role).HasMaxLength(256);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");

                entity.HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Action).HasMaxLength(256);

                entity.Property(e => e.Controller)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.IconPath)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.Order).HasColumnType("int(11)");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasDefaultValueSql("'Admin'");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Product");

                entity.Property(e => e.Id).HasColumnType("int(11)");
            });

            modelBuilder.Entity<SubCategory>(entity =>
            {
                entity.ToTable("SubCategory");

                entity.HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.CategoryId, "SubCategories_FK");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.CategoryId).HasColumnType("int(11)");

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.IconPath)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.Order).HasColumnType("int(11)");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasDefaultValueSql("'Admin'");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.SubCategories)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("SubCategories_FK");
            });

            modelBuilder.Entity<Worker>(entity =>
            {
                entity.ToTable("Worker");

                entity.Property(e => e.Id).HasColumnType("int(11)");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}