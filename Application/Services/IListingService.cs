using multi_tenant_beauty_platform_back.Application.DTOs.Listing;

namespace multi_tenant_beauty_platform_back.Application.Services;

public interface IListingService
{
    Task<IReadOnlyList<SpecialistListItemDto>> GetTopSpecialistsAsync(int count = 10, CancellationToken ct = default);
    Task<IReadOnlyList<SalonListItemDto>> GetTopSalonsAsync(int count = 10, CancellationToken ct = default);
}
