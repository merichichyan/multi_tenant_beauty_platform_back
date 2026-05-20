using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface ITenantRepository
{
    Task<SpecialistProfile> AddSpecialistAsync(SpecialistProfile specialist, CancellationToken cancellationToken = default);
    Task<SalonProfile> AddSalonAsync(SalonProfile salon, CancellationToken cancellationToken = default);
    Task AddServiceAsync(ServiceItem service, CancellationToken cancellationToken = default);
    Task<StaffMember> AddStaffMemberAsync(StaffMember staff, CancellationToken cancellationToken = default);
}
