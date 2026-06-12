using System.Text.Json.Serialization;

namespace multi_tenant_beauty_platform_back.Application.DTOs.Auth;

public record AuthResponseDto(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("isOnboardingCompleted")] bool IsOnboardingCompleted,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("userEmail")] string UserEmail
);
