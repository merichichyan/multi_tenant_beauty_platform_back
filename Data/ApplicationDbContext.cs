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
    public DbSet<User> Users { get; set; }
    public DbSet<Specialist> Specialists { get; set; }
    public DbSet<Salon> Salons { get; set; }
    public DbSet<StaffMember> StaffMembers { get; set; }
    public DbSet<ServiceItem> ServiceItems { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OnboardingSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProgramId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Timezone).HasMaxLength(100);
            entity.Property(e => e.NotificationsAllowed).IsRequired();
            entity.HasIndex(e => e.DeviceId).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.UseTpcMappingStrategy();

            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.IsOnboardingCompleted).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Specialist>(entity =>
        {
            entity.ToTable("Specialists");
            entity.Property(e => e.Address).IsRequired().HasMaxLength(300);
            entity.HasMany(e => e.Services)
                  .WithOne()
                  .HasForeignKey(s => s.SpecialistId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Salon>(entity =>
        {
            entity.ToTable("Salons");
            entity.Property(e => e.Address).IsRequired().HasMaxLength(300);
            entity.Property(e => e.SalonName).IsRequired().HasMaxLength(150);
            entity.HasMany(e => e.StaffMembers)
                  .WithOne()
                  .HasForeignKey(sm => sm.SalonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.Services)
                  .WithOne()
                  .HasForeignKey(s => s.StaffMemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price);
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
    }
}
