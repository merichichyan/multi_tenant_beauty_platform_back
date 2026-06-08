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
        int page, int pageSize, string? query = null, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Specialists
            .Include(s => s.Services)
            .Where(s => s.Status == "Verified")
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            dbQuery = dbQuery.Where(s => s.FullName.ToLower().Contains(q) || s.Email.ToLower().Contains(q));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
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
