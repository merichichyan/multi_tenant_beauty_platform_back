using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.DTOs.Listing;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class ListingService : IListingService
{
    private readonly ApplicationDbContext _db;

    public ListingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SpecialistListItemDto>> GetTopSpecialistsAsync(int count = 10, CancellationToken ct = default)
    {
        var specialists = await _db.Specialists
            .Include(sp => sp.Services)
            .OrderByDescending(sp => sp.Id)
            .Take(count)
            .ToListAsync(ct);

        return specialists.Select(s => new SpecialistListItemDto(
            Id: s.Id,
            UserId: s.Id,
            FullName: s.FullName,
            Address: s.Address,
            LogoUrl: s.LogoUrl,
            Description: s.Description,
            WorkingHours: s.WorkingHours,
            PreferredColors: s.PreferredColors,
            Services: s.Services
                .Select(svc => new ServiceItemDto(svc.Name, svc.Category, svc.Price, svc.DurationMinutes))
                .ToList()
                .AsReadOnly(),
            AverageRating: null,
            ReviewCount: 0
        )).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<SalonListItemDto>> GetTopSalonsAsync(int count = 10, CancellationToken ct = default)
    {
        var salons = await _db.Salons
            .Include(s => s.StaffMembers)
                .ThenInclude(sm => sm.Services)
            .OrderByDescending(s => s.Id)
            .Take(count)
            .ToListAsync(ct);

        return salons.Select(s => new SalonListItemDto(
            Id: s.Id,
            UserId: s.Id,
            SalonName: s.SalonName,
            Address: s.Address,
            LogoUrl: s.LogoUrl,
            Description: s.Description,
            OperatingHours: s.OperatingHours,
            PreferredColors: s.PreferredColors,
            StaffMembers: s.StaffMembers
                .Select(sm => new StaffMemberDto(
                    sm.FullName,
                    sm.Title ?? string.Empty,
                    sm.GraphicsUrl,
                    sm.Services
                        .Select(svc => new ServiceItemDto(svc.Name, svc.Category, svc.Price, svc.DurationMinutes))
                        .ToList()
                        .AsReadOnly()
                ))
                .ToList()
                .AsReadOnly(),
            AverageRating: null,
            ReviewCount: 0
        )).ToList().AsReadOnly();
    }
}
