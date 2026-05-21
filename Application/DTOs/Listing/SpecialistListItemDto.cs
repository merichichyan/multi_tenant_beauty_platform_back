namespace multi_tenant_beauty_platform_back.Application.DTOs.Listing;

public record ServiceItemDto(string Name, string Category, decimal Price, int DurationMinutes);

public record SpecialistListItemDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string Address,
    string? LogoUrl,
    string? Description,
    string? WorkingHours,
    string? PreferredColors,
    IReadOnlyList<ServiceItemDto> Services,
    double? AverageRating,
    int ReviewCount
);
