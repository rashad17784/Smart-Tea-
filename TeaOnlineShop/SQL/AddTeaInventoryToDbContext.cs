using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Data
{
    public partial class AppDbContext
    {
        // Add these properties to your existing DbContext class
        public DbSet<TeaInventoryItem> TeaInventoryItems { get; set; }
        public DbSet<TeaInventoryTransaction> TeaInventoryTransactions { get; set; }
        
        // Add this to your OnModelCreating method
        protected void ConfigureTeaInventory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeaInventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TeaType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Grade).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Origin).HasMaxLength(100);
                entity.Property(e => e.HarvestSeason).HasMaxLength(50);
                entity.Property(e => e.BatchNumber).HasMaxLength(50);
                entity.Property(e => e.CurrentStock).HasColumnType("decimal(18, 2)").IsRequired();
                entity.Property(e => e.Unit).HasMaxLength(10).IsRequired();
                entity.Property(e => e.MinimumStock).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.ReorderQuantity).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.RetailPrice).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.QRCodeData).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.HasBeenCorrected).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.LastCorrectedBy).HasMaxLength(100);
                
                // Create a unique index on QRCodeData
                entity.HasIndex(e => e.QRCodeData).IsUnique();
            });
            
            modelBuilder.Entity<TeaInventoryTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // For backward compatibility, map InventoryItemId to a column named TeaInventoryItemId
                entity.Property(e => e.InventoryItemId).HasColumnName("InventoryItemId");
                
                entity.Property(e => e.TransactionDate).IsRequired();
                entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)").IsRequired();
                entity.Property(e => e.PreviousStock).HasColumnType("decimal(18, 2)").IsRequired();
                entity.Property(e => e.NewStock).HasColumnType("decimal(18, 2)").IsRequired();
                entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
                entity.Property(e => e.PerformedBy).HasMaxLength(100);
                entity.Property(e => e.IsCorrection).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.QRCodeScanned).HasMaxLength(100);
                
                // Configure the relationship
                entity.HasOne(e => e.InventoryItem)
                      .WithMany(i => i.Transactions)
                      .HasForeignKey(e => e.InventoryItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 