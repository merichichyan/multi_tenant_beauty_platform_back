namespace multi_tenant_beauty_platform_back.Application.DTOs.Listing;

public record StaffMemberDto(string FullName, string Title, string? GraphicsUrl, IReadOnlyList<ServiceItemDto> Services);

public record SalonListItemDto(
    Guid Id,
    Guid UserId,
    string SalonName,
    string Address,
    string? LogoUrl,
    string? Description,
    string? OperatingHours,
    string? PreferredColors,
    IReadOnlyList<StaffMemberDto> StaffMembers,
    double? AverageRating,
    int ReviewCount
);
