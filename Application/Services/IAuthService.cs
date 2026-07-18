using multi_tenant_beauty_platform_back.Application.DTOs.Auth;

namespace multi_tenant_beauty_platform_back.Application.Services;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<RegisterResponseDto> RegisterUserAsync(UserRegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<RegisterResponseDto> RegisterSpecialistAsync(SpecialistRegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<RegisterResponseDto> RegisterSalonAsync(SalonRegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task CompleteOnboardingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SelectRoleAsync(SelectRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ActivateAccountAsync(ActivateRequestDto request, CancellationToken cancellationToken = default);
}
