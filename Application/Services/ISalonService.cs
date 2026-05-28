using multi_tenant_beauty_platform_back.Application.DTOs;

namespace multi_tenant_beauty_platform_back.Application.Services;

public interface ISalonService
{
    Task<PaginatedResponseDto<SalonListItemDto>> GetPagedAsync(int page, CancellationToken ct = default);
    Task<SalonListItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SalonListItemDto>> GetClosestAsync(double latitude, double longitude, int limit, Guid? categoryId = null, CancellationToken ct = default);
}
