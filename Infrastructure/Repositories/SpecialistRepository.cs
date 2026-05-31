using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Infrastructure.Repositories;

public class SpecialistRepository : ISpecialistRepository
{
    private readonly ApplicationDbContext _context;

    public SpecialistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Specialist> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Specialists
            .Include(s => s.Services)
            .Where(s => s.Status == "Verified")
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Specialist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Specialists
            .Include(s => s.Services)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
