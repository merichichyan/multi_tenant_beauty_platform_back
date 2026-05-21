using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface ISalonRepository
{
    Task<(IEnumerable<SalonProfile> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SalonProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
