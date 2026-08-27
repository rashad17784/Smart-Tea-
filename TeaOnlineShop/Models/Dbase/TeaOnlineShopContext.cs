using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TeaOnlineShop.Models.Dbase;

public partial class TeaOnlineShopContext : DbContext
{
    public TeaOnlineShopContext()
    {
    }

    public TeaOnlineShopContext(DbContextOptions<TeaOnlineShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Banner> Banners { get; set; }

    public virtual DbSet<CommentSection> CommentSections { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductGallery> ProductGalleries { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierCategory> SupplierCategories { get; set; }

    public virtual DbSet<SupplierCategoryMapping> SupplierCategoryMappings { get; set; }

    public virtual DbSet<SupplyItem> SupplyItems { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<DeliveryItem> DeliveryItems { get; set; }

    public virtual DbSet<QRCodeScan> QRCodeScans { get; set; }
    
    public virtual DbSet<TeaInventoryItem> TeaInventoryItems { get; set; }
    
    public virtual DbSet<TeaInventoryTransaction> TeaInventoryTransactions { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }
    public virtual DbSet<WarehouseBin> WarehouseBins { get; set; }
    public virtual DbSet<ProductInventoryMapping> ProductInventoryMappings { get; set; }
    public virtual DbSet<OrderLine> OrderLines { get; set; }
    public virtual DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
    public virtual DbSet<OrderPaymentEvent> OrderPaymentEvents { get; set; }
    public virtual DbSet<AiPredictionHistory> AiPredictionHistories { get; set; }
    public virtual DbSet<StockLedgerEntry> StockLedgerEntries { get; set; }
    public virtual DbSet<StockBalance> StockBalances { get; set; }
    public virtual DbSet<InventoryImportBatch> InventoryImportBatches { get; set; }
    public virtual DbSet<InventoryImportRow> InventoryImportRows { get; set; }
    public virtual DbSet<InventoryImportRowError> InventoryImportRowErrors { get; set; }
    public virtual DbSet<StockReconciliation> StockReconciliations { get; set; }
    public virtual DbSet<StockReconciliationLine> StockReconciliationLines { get; set; }
    public virtual DbSet<OperationalDataImportBatch> OperationalDataImportBatches { get; set; }
    public virtual DbSet<OperationalDataImportRow> OperationalDataImportRows { get; set; }
    public virtual DbSet<OperationalDataImportRowError> OperationalDataImportRowErrors { get; set; }
    public virtual DbSet<OperationalDataImportAuditEvent> OperationalDataImportAuditEvents { get; set; }
    public virtual DbSet<OperationalInventoryEvent> OperationalInventoryEvents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=TeaOnlineShop;Trusted_Connection=True;TrustServerCertificate=true");
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.ToTable("Banner");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ImageName).HasMaxLength(50);
            entity.Property(e => e.Link).HasMaxLength(100);
            entity.Property(e => e.Positon).HasMaxLength(50);
            entity.Property(e => e.SubTitle).HasMaxLength(1000);
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsFixedLength();
        });

        modelBuilder.Entity<CommentSection>(entity =>
        {
            entity.ToTable("CommentSection");

            entity.Property(e => e.CommmentText).HasMaxLength(1200);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Link).HasMaxLength(300);
            entity.Property(e => e.MenuTitle).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(30);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Order");

            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Comment).HasMaxLength(250);
            entity.Property(e => e.Country).HasMaxLength(50);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Shipping).HasColumnType("money");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasMaxLength(40).IsRequired();
            entity.Property(e => e.PaymentStatus).HasMaxLength(40).IsRequired();
            entity.Property(e => e.ShippedByName).HasMaxLength(120);
            entity.Property(e => e.SubTotal).HasColumnType("money");
            entity.Property(e => e.Total).HasColumnType("money");
            entity.Property(e => e.TransId).HasMaxLength(200);
            entity.HasIndex(e => e.TransId).HasDatabaseName("IX_Order_TransId");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Discount).HasColumnType("money");
            entity.Property(e => e.FullDescription).HasMaxLength(4000);
            entity.Property(e => e.ImageName).HasMaxLength(75);
            entity.Property(e => e.Price).HasColumnType("money");
            entity.Property(e => e.Tags).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.VideoUrl).HasMaxLength(400);
        });

        modelBuilder.Entity<ProductGallery>(entity =>
        {
            entity.ToTable("ProductGallery");

            entity.Property(e => e.ImageName).HasMaxLength(70);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.Property(e => e.Shipping).HasColumnType("money");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.DateOfRegister).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(50);
            entity.Property(e => e.UserRole).HasMaxLength(50).HasDefaultValue("Customer");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Supplier");

            entity.Property(e => e.SupplierCode).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.RegistrationDate).HasColumnType("datetime");
            entity.Property(e => e.QRCodeData).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        modelBuilder.Entity<SupplierCategory>(entity =>
        {
            entity.ToTable("SupplierCategory");

            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<SupplierCategoryMapping>(entity =>
        {
            entity.ToTable("SupplierCategoryMapping");

            entity.HasOne(d => d.Supplier)
                .WithMany(p => p.CategoryMappings)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.SupplierMappings)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.ToTable("Delivery");

            entity.Property(e => e.DeliveryCode).HasMaxLength(50);
            entity.Property(e => e.DeliveryDate).HasColumnType("datetime");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReceivedByName).HasMaxLength(120).HasDefaultValue(string.Empty);

            entity.HasOne(d => d.Supplier)
                .WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull);

        });

        modelBuilder.Entity<SupplyItem>(entity =>
        {
            entity.ToTable("SupplyItem");

            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MinimumStock).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CurrentStock).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ItemCode).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.ItemCode).IsUnique();
        });

        modelBuilder.Entity<DeliveryItem>(entity =>
        {
            entity.ToTable("DeliveryItem");

            entity.Property(e => e.Quantity).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Delivery)
                .WithMany(p => p.DeliveryItems)
                .HasForeignKey(d => d.DeliveryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Item)
                .WithMany(p => p.DeliveryItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<QRCodeScan>(entity =>
        {
            entity.ToTable("QRCodeScan");

            entity.Property(e => e.QRCodeData).HasMaxLength(255);
            entity.Property(e => e.ScanDateTime).HasColumnType("datetime");
            entity.Property(e => e.ScanResult).HasMaxLength(50);
            entity.Property(e => e.ActionTaken).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ScannedByName).HasMaxLength(120).HasDefaultValue(string.Empty);
            entity.Property(e => e.EntityType).HasMaxLength(40).HasDefaultValue(string.Empty);
            entity.Property(e => e.IpAddress).HasMaxLength(64).HasDefaultValue(string.Empty);
            entity.Property(e => e.UserAgent).HasMaxLength(500).HasDefaultValue(string.Empty);
            entity.HasIndex(e => e.ScanDateTime);
            entity.HasIndex(e => e.CorrelationId).IsUnique();
        });

        modelBuilder.Entity<TeaInventoryItem>(entity =>
        {
            entity.ToTable("TeaInventoryItems");

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TeaType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Grade).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.QRCodeData).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ItemCode).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.ItemCode).IsUnique();
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").IsRequired();
            
            // Set default empty string for nullable string properties
            entity.Property(e => e.Origin).HasMaxLength(100).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.HarvestSeason).HasMaxLength(50).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.BatchNumber).HasMaxLength(50).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.Description).HasMaxLength(500).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.LastCorrectedBy).HasMaxLength(100).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.CorrectionReason).HasMaxLength(500).HasDefaultValue(string.Empty).IsRequired(false);
            
            // Numeric and date configurations
            entity.Property(e => e.HarvestDate).HasColumnType("datetime");
            entity.Property(e => e.MinimumStock).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CurrentStock).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ReorderQuantity).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RetailPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.LastCorrectionDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TeaInventoryTransaction>(entity =>
        {
            entity.ToTable("TeaInventoryTransactions");

            entity.Property(e => e.InventoryItemId).HasColumnName("InventoryItemId").IsRequired();
            entity.Property(e => e.TransactionDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(10, 2)").IsRequired();
            entity.Property(e => e.PreviousStock).HasColumnType("decimal(10, 2)").IsRequired();
            entity.Property(e => e.NewStock).HasColumnType("decimal(10, 2)").IsRequired();
            
            // Required string with default
            entity.Property(e => e.TransactionType).HasMaxLength(50).IsRequired().HasDefaultValue(string.Empty);
            
            // Nullable strings with defaults
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.Notes).HasMaxLength(500).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.PerformedBy).HasMaxLength(100).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.CorrectionReason).HasMaxLength(500).HasDefaultValue(string.Empty).IsRequired(false);
            entity.Property(e => e.QRCodeScanned).HasMaxLength(255).HasDefaultValue(string.Empty).IsRequired(false);
            
            // Numeric configurations
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");
            
            // Skip properties handled by compatibility layer
            entity.Ignore(e => e.TeaInventoryItemId);
            entity.Ignore(e => e.TeaInventoryItem);

            entity.HasOne(d => d.InventoryItem)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.InventoryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouse");
            entity.Property(e => e.Code).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<WarehouseBin>(entity =>
        {
            entity.ToTable("WarehouseBin");
            entity.Property(e => e.Code).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(e => new { e.WarehouseId, e.Code }).IsUnique();
            entity.HasOne(e => e.Warehouse)
                .WithMany(e => e.Bins)
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductInventoryMapping>(entity =>
        {
            entity.ToTable("ProductInventoryMapping");
            entity.Property(e => e.QuantityPerUnit).HasColumnType("decimal(18, 4)");
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.HasIndex(e => e.InventoryItemId);
            entity.HasOne(e => e.Product)
                .WithOne(e => e.InventoryMapping)
                .HasForeignKey<ProductInventoryMapping>(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InventoryItem)
                .WithMany(e => e.ProductMappings)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("OrderLine");
            entity.Property(e => e.Sku).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FulfilmentStatus).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => e.OrderId);
            entity.HasOne(e => e.Order)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                .WithMany(e => e.OrderLines)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistory");
            entity.Property(e => e.FromStatus).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ToStatus).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ChangedByName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => new { e.OrderId, e.ChangedAtUtc });
            entity.HasOne(e => e.Order)
                .WithMany(e => e.StatusHistory)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderPaymentEvent>(entity =>
        {
            entity.ToTable("OrderPaymentEvent");
            entity.Property(e => e.FromStatus).HasMaxLength(40).IsRequired();
            entity.Property(e => e.ToStatus).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Method).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Reference).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RecordedByName).HasMaxLength(120).IsRequired();
            entity.HasIndex(e => new { e.OrderId, e.RecordedAtUtc });
            entity.HasOne(e => e.Order)
                .WithMany(e => e.PaymentEvents)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiPredictionHistory>(entity =>
        {
            entity.ToTable("AiPredictionHistory");
            entity.Property(e => e.PredictionType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Grade).HasMaxLength(20).IsRequired();
            entity.Property(e => e.RequestedByName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(120).IsRequired();
            entity.Property(e => e.ModelVersion).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Strategy).HasMaxLength(80).IsRequired();
            entity.Property(e => e.ExpectedMape).HasColumnType("decimal(9, 4)");
            entity.Property(e => e.DataSource).HasMaxLength(120).IsRequired();
            entity.Property(e => e.SourceLabel).HasMaxLength(160).IsRequired();
            entity.Property(e => e.SourceNote).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.InputSummary).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ResultJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.PublicId).IsUnique();
            entity.HasIndex(e => new { e.PredictionType, e.RequestedAtUtc });
            entity.HasIndex(e => new { e.Grade, e.RequestedAtUtc });
        });

        modelBuilder.Entity<StockLedgerEntry>(entity =>
        {
            entity.ToTable("StockLedgerEntry");
            entity.Property(e => e.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MovementType).HasMaxLength(40).IsRequired();
            entity.Property(e => e.QuantityChange).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.PreviousStock).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.NewStock).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ReferenceType).HasMaxLength(40).IsRequired();
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PerformedByName).HasMaxLength(120).IsRequired();
            entity.HasIndex(e => e.EntryNumber).IsUnique();
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.OccurredAtUtc);
            entity.HasIndex(e => new { e.InventoryItemId, e.OccurredAtUtc });
            entity.HasIndex(e => new { e.SupplyItemId, e.OccurredAtUtc });
            entity.HasOne(e => e.Warehouse)
                .WithMany(e => e.LedgerEntries)
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin)
                .WithMany()
                .HasForeignKey(e => e.BinId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItem)
                .WithMany(e => e.LedgerEntries)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SupplyItem)
                .WithMany(e => e.LedgerEntries)
                .HasForeignKey(e => e.SupplyItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReversesEntry)
                .WithMany()
                .HasForeignKey(e => e.ReversesEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("StockBalance");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.WarehouseId, e.BinId, e.InventoryItemId })
                .HasFilter("[InventoryItemId] IS NOT NULL")
                .IsUnique();
            entity.HasIndex(e => new { e.WarehouseId, e.BinId, e.SupplyItemId })
                .HasFilter("[SupplyItemId] IS NOT NULL")
                .IsUnique();
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin)
                .WithMany()
                .HasForeignKey(e => e.BinId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryImportBatch>(entity =>
        {
            entity.ToTable("InventoryImportBatch");
            entity.Property(e => e.ImportType).HasMaxLength(40).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(260).IsRequired();
            entity.Property(e => e.FileSha256).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.SubmittedByName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.ApprovedByName).HasMaxLength(120);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.FileSha256);
            entity.HasIndex(e => e.SubmittedAtUtc);
        });

        modelBuilder.Entity<InventoryImportRow>(entity =>
        {
            entity.ToTable("InventoryImportRow");
            entity.Property(e => e.ItemType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.WarehouseCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.BinCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => new { e.BatchId, e.RowNumber }).IsUnique();
            entity.HasOne(e => e.Batch)
                .WithMany(e => e.Rows)
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LedgerEntry)
                .WithMany()
                .HasForeignKey(e => e.LedgerEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryImportRowError>(entity =>
        {
            entity.ToTable("InventoryImportRowError");
            entity.Property(e => e.FieldName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.ErrorCode).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => new { e.BatchId, e.RowNumber });
            entity.HasOne(e => e.Batch)
                .WithMany(e => e.Errors)
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockReconciliation>(entity =>
        {
            entity.ToTable("StockReconciliation");
            entity.Property(e => e.ReconciliationNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.CreatedByName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.ApprovedByName).HasMaxLength(120);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.ReconciliationNumber).IsUnique();
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockReconciliationLine>(entity =>
        {
            entity.ToTable("StockReconciliationLine");
            entity.Property(e => e.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SystemQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CountedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Difference).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.ReconciliationId);
            entity.HasOne(e => e.Reconciliation)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.ReconciliationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LedgerEntry)
                .WithMany()
                .HasForeignKey(e => e.LedgerEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationalDataImportBatch>(entity =>
        {
            entity.ToTable("OperationalDataImportBatch");
            entity.Property(e => e.BatchNumber).HasMaxLength(40).IsRequired();
            entity.Property(e => e.SourceSystem).HasMaxLength(80).IsRequired();
            entity.Property(e => e.SourceDocumentReference).HasMaxLength(120).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(260).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FileSha256).HasMaxLength(64).IsRequired();
            entity.Property(e => e.OriginalFile).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.SubmittedByName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.ApprovedByName).HasMaxLength(120);
            entity.Property(e => e.RejectedByName).HasMaxLength(120);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.ExpectedInboundKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ExpectedOutboundKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CalculatedInboundKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CalculatedOutboundKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ReconciliationStatus).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.BatchNumber).IsUnique();
            entity.HasIndex(e => e.FileSha256).IsUnique();
            entity.HasIndex(e => new { e.SourceSystem, e.SourcePeriodStartUtc, e.SourcePeriodEndUtc });
            entity.HasIndex(e => new { e.Status, e.SubmittedAtUtc });
        });

        modelBuilder.Entity<OperationalDataImportRow>(entity =>
        {
            entity.ToTable("OperationalDataImportRow");
            entity.Property(e => e.SourceSystem).HasMaxLength(80).IsRequired();
            entity.Property(e => e.SourceRecordId).HasMaxLength(120).IsRequired();
            entity.Property(e => e.TeaGrade).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.QuantityKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.OriginalUnit).HasMaxLength(20).IsRequired();
            entity.Property(e => e.TransactionType).HasMaxLength(40).IsRequired();
            entity.Property(e => e.QuantityChangeKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SourceReferenceNumber).HasMaxLength(120).IsRequired();
            entity.Property(e => e.SupplierOrProductionReference).HasMaxLength(120).IsRequired();
            entity.Property(e => e.WarehouseCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.BinCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CanonicalSha256).HasMaxLength(64).IsRequired();
            entity.Property(e => e.RawData).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => new { e.BatchId, e.RowNumber }).IsUnique();
            entity.HasIndex(e => new { e.SourceSystem, e.SourceRecordId });
            entity.HasIndex(e => e.CanonicalSha256);
            entity.HasOne(e => e.Batch).WithMany(e => e.Rows).HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItem).WithMany().HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationalDataImportRowError>(entity =>
        {
            entity.ToTable("OperationalDataImportRowError");
            entity.Property(e => e.FieldName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.ErrorCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => new { e.BatchId, e.RowNumber });
            entity.HasOne(e => e.Batch).WithMany(e => e.Errors).HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationalDataImportAuditEvent>(entity =>
        {
            entity.ToTable("OperationalDataImportAuditEvent");
            entity.Property(e => e.Action).HasMaxLength(60).IsRequired();
            entity.Property(e => e.FromStatus).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ToStatus).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ActorName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Details).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(e => new { e.BatchId, e.OccurredAtUtc });
            entity.HasOne(e => e.Batch).WithMany(e => e.AuditEvents).HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationalInventoryEvent>(entity =>
        {
            entity.ToTable("OperationalInventoryEvent");
            entity.Property(e => e.SourceSystem).HasMaxLength(80).IsRequired();
            entity.Property(e => e.SourceRecordId).HasMaxLength(120).IsRequired();
            entity.Property(e => e.TeaGrade).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.QuantityKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.QuantityChangeKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TransactionType).HasMaxLength(40).IsRequired();
            entity.Property(e => e.SourceReferenceNumber).HasMaxLength(120).IsRequired();
            entity.Property(e => e.SupplierOrProductionReference).HasMaxLength(120).IsRequired();
            entity.Property(e => e.WarehouseCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.BinCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CanonicalSha256).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ImportedByName).HasMaxLength(120).IsRequired();
            entity.HasIndex(e => e.PublicId).IsUnique();
            entity.HasIndex(e => new { e.SourceSystem, e.SourceRecordId }).IsUnique();
            entity.HasIndex(e => e.ImportRowId).IsUnique();
            entity.HasIndex(e => new { e.TeaGrade, e.IsDemand, e.SourceOccurredAtUtc });
            entity.HasIndex(e => new { e.BatchId, e.SourceOccurredAtUtc });
            entity.HasOne(e => e.Batch).WithMany(e => e.PublishedEvents).HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ImportRow).WithOne().HasForeignKey<OperationalInventoryEvent>(e => e.ImportRowId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItem).WithMany().HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
