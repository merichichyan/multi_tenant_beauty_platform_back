using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface ISpecialistRepository
{
    Task<(IEnumerable<Specialist> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? query = null, CancellationToken cancellationToken = default);
    Task<Specialist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
