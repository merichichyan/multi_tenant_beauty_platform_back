using System.Text.Json.Serialization;

namespace multi_tenant_beauty_platform_back.Application.DTOs.Auth;

public record UserRegisterRequestDto(
    [property: JsonPropertyName("phone")] string Phone,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("fullName")] string? FullName = null,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("role")] string? Role = "user",
    [property: JsonPropertyName("gender")] string? Gender = null,
    [property: JsonPropertyName("birthday")] DateTime? Birthday = null,
    [property: JsonPropertyName("deviceId")] string? DeviceId = null
);
