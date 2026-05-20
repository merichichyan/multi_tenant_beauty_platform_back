using System.Text.Json.Serialization;

namespace multi_tenant_beauty_platform_back.Application.DTOs.Auth;

public record RegisterResponseDto(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("isOnboardingCompleted")] bool IsOnboardingCompleted
);
