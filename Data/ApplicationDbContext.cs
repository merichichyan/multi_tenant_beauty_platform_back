using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<OnboardingSubmission> OnboardingSubmissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OnboardingSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProgramId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Timezone).HasMaxLength(100);
            entity.HasIndex(e => e.DeviceId).IsUnique(); // One active onboarding per device/user in this design
        });
    }
}
