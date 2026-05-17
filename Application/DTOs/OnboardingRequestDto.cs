namespace multi_tenant_beauty_platform_back.Application.DTOs;

public record OnboardingRequestDto(
    string DeviceId,
    string ProgramId,
    string Language,
    string Role,
    string Timezone
);
