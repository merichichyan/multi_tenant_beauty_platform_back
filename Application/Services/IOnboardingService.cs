using multi_tenant_beauty_platform_back.Application.DTOs;

namespace multi_tenant_beauty_platform_back.Application.Services;

public interface IOnboardingService
{
    Task<OnboardingResponseDto> SubmitOnboardingAsync(OnboardingRequestDto request, CancellationToken cancellationToken = default);
    Task<OnboardingResponseDto> GetOnboardingByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OnboardingResponseDto>> GetAllOnboardingsAsync(CancellationToken cancellationToken = default);
}
