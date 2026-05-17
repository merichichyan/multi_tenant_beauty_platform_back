using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Infrastructure.Repositories;

public class OnboardingRepository : IOnboardingRepository
{
    private readonly ApplicationDbContext _context;

    public OnboardingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OnboardingSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OnboardingSubmissions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<OnboardingSubmission?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.OnboardingSubmissions
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
    }

    public async Task<IEnumerable<OnboardingSubmission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OnboardingSubmissions.ToListAsync(cancellationToken);
    }

    public async Task<OnboardingSubmission> AddAsync(OnboardingSubmission submission, CancellationToken cancellationToken = default)
    {
        await _context.OnboardingSubmissions.AddAsync(submission, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return submission;
    }

    public async Task UpdateAsync(OnboardingSubmission submission, CancellationToken cancellationToken = default)
    {
        _context.OnboardingSubmissions.Update(submission);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
