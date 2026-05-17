using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Repositories;

public interface IOnboardingRepository
{
    Task<OnboardingSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OnboardingSubmission?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OnboardingSubmission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OnboardingSubmission> AddAsync(OnboardingSubmission submission, CancellationToken cancellationToken = default);
    Task UpdateAsync(OnboardingSubmission submission, CancellationToken cancellationToken = default);
}
