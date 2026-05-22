using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface ITenantRepository
{
    Task<Specialist> AddSpecialistAsync(Specialist specialist, CancellationToken cancellationToken = default);
    Task<Salon> AddSalonAsync(Salon salon, CancellationToken cancellationToken = default);
    Task AddServiceAsync(ServiceItem service, CancellationToken cancellationToken = default);
    Task<StaffMember> AddStaffMemberAsync(StaffMember staff, CancellationToken cancellationToken = default);
}
