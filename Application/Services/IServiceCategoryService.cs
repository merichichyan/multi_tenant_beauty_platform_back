using multi_tenant_beauty_platform_back.Application.DTOs;

namespace multi_tenant_beauty_platform_back.Application.Services;

public interface IServiceCategoryService
{
    Task<IEnumerable<ServiceCategoryResponseDto>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceCategoryResponseDto> CreateAsync(ServiceCategoryRequestDto dto, CancellationToken ct = default);
    Task<ServiceCategoryResponseDto?> UpdateAsync(Guid id, ServiceCategoryRequestDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
