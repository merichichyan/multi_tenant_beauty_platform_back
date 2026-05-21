using System.Text.Json.Serialization;

namespace multi_tenant_beauty_platform_back.Application.DTOs.Auth;

public record UserRegisterRequestDto(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("gender")] string? Gender,
    [property: JsonPropertyName("birthday")] DateTime? Birthday,
    [property: JsonPropertyName("deviceId")] string? DeviceId
);
