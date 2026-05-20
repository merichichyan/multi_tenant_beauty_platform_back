using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface IServiceCategoryRepository
{
    Task<IEnumerable<ServiceCategory>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ServiceCategory category, CancellationToken ct = default);
    Task UpdateAsync(ServiceCategory category, CancellationToken ct = default);
    Task DeleteAsync(ServiceCategory category, CancellationToken ct = default);
}
