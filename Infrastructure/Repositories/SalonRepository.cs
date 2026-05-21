using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Infrastructure.Repositories;

public class SalonRepository : ISalonRepository
{
    private readonly ApplicationDbContext _context;

    public SalonRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<SalonProfile> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.SalonProfiles
            .Include(s => s.StaffMembers)
                .ThenInclude(sm => sm.Services)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SalonProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SalonProfiles
            .Include(s => s.StaffMembers)
                .ThenInclude(sm => sm.Services)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
