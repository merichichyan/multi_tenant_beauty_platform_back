using System.Text.Json.Serialization;

namespace multi_tenant_beauty_platform_back.Application.DTOs.Auth;

public record RegisterRequestDto(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("fullName")] string FullName
);
