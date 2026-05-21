namespace multi_tenant_beauty_platform_back.Application.DTOs;

public record OnboardingResponseDto(
    Guid Id,
    string DeviceId,
    string ProgramId,
    string Language,
    string Timezone,
    bool NotificationsAllowed,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
