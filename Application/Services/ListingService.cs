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
        var specialists = await _db.SpecialistProfiles
            .Include(sp => sp.Services)
            .OrderByDescending(sp => sp.Id) // newest first until we have ratings
            .Take(count)
            .Join(
                _db.Users,
                sp => sp.UserId,
                u => u.Id,
                (sp, u) => new { sp, u }
            )
            .ToListAsync(ct);

        return specialists.Select(x => new SpecialistListItemDto(
            Id: x.sp.Id,
            UserId: x.sp.UserId,
            FullName: x.u.FullName,
            Address: x.sp.Address,
            LogoUrl: x.sp.LogoUrl,
            Description: x.sp.Description,
            WorkingHours: x.sp.WorkingHours,
            PreferredColors: x.sp.PreferredColors,
            Services: x.sp.Services
                .Select(s => new ServiceItemDto(s.Name, s.Category, s.Price, s.DurationMinutes))
                .ToList()
                .AsReadOnly(),
            AverageRating: null,   // placeholder until ratings feature exists
            ReviewCount: 0
        )).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<SalonListItemDto>> GetTopSalonsAsync(int count = 10, CancellationToken ct = default)
    {
        var salons = await _db.SalonProfiles
            .Include(s => s.StaffMembers)
                .ThenInclude(sm => sm.Services)
            .OrderByDescending(s => s.Id)
            .Take(count)
            .Join(
                _db.Users,
                s => s.UserId,
                u => u.Id,
                (s, u) => new { s, u }
            )
            .ToListAsync(ct);

        return salons.Select(x => new SalonListItemDto(
            Id: x.s.Id,
            UserId: x.s.UserId,
            SalonName: x.s.SalonName,
            Address: x.s.Address,
            LogoUrl: x.s.LogoUrl,
            Description: x.s.Description,
            OperatingHours: x.s.OperatingHours,
            PreferredColors: x.s.PreferredColors,
            StaffMembers: x.s.StaffMembers
                .Select(sm => new StaffMemberDto(
                    sm.FullName,
                    sm.Title ?? string.Empty,
                    sm.GraphicsUrl,
                    sm.Services
                        .Select(s => new ServiceItemDto(s.Name, s.Category, s.Price, s.DurationMinutes))
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
