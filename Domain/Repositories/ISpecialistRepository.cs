using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface ISpecialistRepository
{
    Task<(IEnumerable<SpecialistProfile> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SpecialistProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
