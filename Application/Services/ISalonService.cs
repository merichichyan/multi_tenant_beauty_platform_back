using multi_tenant_beauty_platform_back.Application.DTOs;

namespace multi_tenant_beauty_platform_back.Application.Services;

public interface ISalonService
{
    Task<PaginatedResponseDto<SalonListItemDto>> GetPagedAsync(int page, CancellationToken ct = default);
    Task<SalonListItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
