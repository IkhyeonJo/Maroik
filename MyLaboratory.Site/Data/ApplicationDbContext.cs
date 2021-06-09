using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MyLaboratory.Site.Data
{
     public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
     {
        #region (For Migration) DbSet<T>'s T is table type.

        public DbSet<Worker> Worker { get; set; }

        public DbSet<Product> Product { get; set; }

        public DbSet<Bill> Bill { get; set; }

        public DbSet<BillType> BillType { get; set; }

        public DbSet<Branch> Branch { get; set; }

        public DbSet<CashBank> CashBank { get; set; }

        public DbSet<Currency> Currency { get; set; }

        public DbSet<Customer> Customer { get; set; }

        public DbSet<CustomerType> CustomerType { get; set; }

        public DbSet<GoodsReceivedNote> GoodsReceivedNote { get; set; }

        public DbSet<Invoice> Invoice { get; set; }

        public DbSet<InvoiceType> InvoiceType { get; set; }

        public DbSet<NumberSequence> NumberSequence { get; set; }

        public DbSet<PaymentReceive> PaymentReceive { get; set; }

        public DbSet<PaymentType> PaymentType { get; set; }

        public DbSet<PaymentVoucher> PaymentVoucher { get; set; }

        public DbSet<ProductType> ProductType { get; set; }

        public DbSet<PurchaseOrder> PurchaseOrder { get; set; }

        public DbSet<PurchaseOrderLine> PurchaseOrderLine { get; set; }

        public DbSet<PurchaseType> PurchaseType { get; set; }

        public DbSet<SalesOrder> SalesOrder { get; set; }

        public DbSet<SalesOrderLine> SalesOrderLine { get; set; }

        public DbSet<SalesType> SalesType { get; set; }

        public DbSet<Shipment> Shipment { get; set; }

        public DbSet<ShipmentType> ShipmentType { get; set; }

        public DbSet<UnitOfMeasure> UnitOfMeasure { get; set; }

        public DbSet<Vendor> Vendor { get; set; }

        public DbSet<VendorType> VendorType { get; set; }

        public DbSet<Warehouse> Warehouse { get; set; }

        public DbSet<UserProfile> UserProfile { get; set; }
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