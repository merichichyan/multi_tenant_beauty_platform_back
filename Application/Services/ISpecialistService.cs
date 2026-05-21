using multi_tenant_beauty_platform_back.Application.DTOs;

namespace multi_tenant_beauty_platform_back.Application.Services;

public interface ISpecialistService
{
    Task<PaginatedResponseDto<SpecialistListItemDto>> GetPagedAsync(int page, CancellationToken ct = default);
    Task<SpecialistListItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
