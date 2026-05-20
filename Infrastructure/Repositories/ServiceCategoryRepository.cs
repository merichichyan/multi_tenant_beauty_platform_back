using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Infrastructure.Repositories;

public class ServiceCategoryRepository : IServiceCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceCategory>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ServiceCategories.ToListAsync(ct);
    }

    public async Task<ServiceCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ServiceCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task AddAsync(ServiceCategory category, CancellationToken ct = default)
    {
        await _context.ServiceCategories.AddAsync(category, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ServiceCategory category, CancellationToken ct = default)
    {
        _context.ServiceCategories.Update(category);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ServiceCategory category, CancellationToken ct = default)
    {
        _context.ServiceCategories.Remove(category);
        await _context.SaveChangesAsync(ct);
    }
}
