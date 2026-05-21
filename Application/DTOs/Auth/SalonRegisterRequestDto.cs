using System.Text.Json.Serialization;

namespace multi_tenant_beauty_platform_back.Application.DTOs.Auth;

public record StaffMemberDto(
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("graphicsUrl")] string? GraphicsUrl,
    [property: JsonPropertyName("services")] List<ServiceDto> Services
);

public record SalonRegisterRequestDto(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("salonName")] string SalonName,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("phone")] string Phone,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("latitude")] double? Latitude,
    [property: JsonPropertyName("longitude")] double? Longitude,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("socialMedias")] string? SocialMedias,
    [property: JsonPropertyName("logoUrl")] string? LogoUrl,
    [property: JsonPropertyName("preferredColors")] string? PreferredColors,
    [property: JsonPropertyName("operatingHours")] string? OperatingHours,
    [property: JsonPropertyName("staffMembers")] List<StaffMemberDto> StaffMembers,
    [property: JsonPropertyName("deviceId")] string? DeviceId
);
