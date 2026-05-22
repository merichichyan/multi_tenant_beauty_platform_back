using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface ISalonRepository
{
    Task<(IEnumerable<Salon> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Salon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
