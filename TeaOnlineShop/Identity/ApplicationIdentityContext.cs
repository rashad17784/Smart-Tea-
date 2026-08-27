using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TeaOnlineShop.Identity;

public sealed class ApplicationIdentityContext
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationIdentityContext(DbContextOptions<ApplicationIdentityContext> options)
        : base(options)
    {
    }

    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.LegacyUserId).IsUnique().HasFilter("[LegacyUserId] IS NOT NULL");
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<LoginHistory>(entity =>
        {
            entity.ToTable("LoginHistory");
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(200);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<SecurityAuditEvent>(entity =>
        {
            entity.ToTable("SecurityAuditEvent");
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ActorEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.TargetEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(1000);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.ActorUserId);
            entity.HasIndex(x => x.TargetUserId);
        });
    }
}
