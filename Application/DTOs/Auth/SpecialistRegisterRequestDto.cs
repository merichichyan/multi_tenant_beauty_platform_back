using System.Text.Json.Serialization;

namespace multi_tenant_beauty_platform_back.Application.DTOs.Auth;

public record ServiceDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("durationMinutes")] int DurationMinutes
);

public record SpecialistRegisterRequestDto(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("phone")] string Phone,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("latitude")] double? Latitude,
    [property: JsonPropertyName("longitude")] double? Longitude,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("socialMedias")] string? SocialMedias,
    [property: JsonPropertyName("logoUrl")] string? LogoUrl,
    [property: JsonPropertyName("preferredColors")] string? PreferredColors,
    [property: JsonPropertyName("workingHours")] string? WorkingHours,
    [property: JsonPropertyName("services")] List<ServiceDto> Services,
    [property: JsonPropertyName("deviceId")] string? DeviceId
);
