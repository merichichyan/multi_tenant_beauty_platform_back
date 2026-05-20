using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SpecialistProfile> AddSpecialistAsync(SpecialistProfile specialist, CancellationToken cancellationToken = default)
    {
        await _context.SpecialistProfiles.AddAsync(specialist, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return specialist;
    }

    public async Task<SalonProfile> AddSalonAsync(SalonProfile salon, CancellationToken cancellationToken = default)
    {
        await _context.SalonProfiles.AddAsync(salon, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return salon;
    }

    public async Task AddServiceAsync(ServiceItem service, CancellationToken cancellationToken = default)
    {
        await _context.ServiceItems.AddAsync(service, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StaffMember> AddStaffMemberAsync(StaffMember staff, CancellationToken cancellationToken = default)
    {
        await _context.StaffMembers.AddAsync(staff, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return staff;
    }
}
