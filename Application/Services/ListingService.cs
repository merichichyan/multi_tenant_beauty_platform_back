using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.DTOs.Listing;
using multi_tenant_beauty_platform_back.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class ListingService : IListingService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ListingService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetLanguage()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return "en";

        if (context.Request.Query.TryGetValue("lang", out var langVal) && !string.IsNullOrEmpty(langVal))
        {
            var lang = langVal.ToString().ToLower();
            if (lang == "hy" || lang == "ru" || lang == "en") return lang;
        }

        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            if (acceptLanguage.Contains("hy", StringComparison.OrdinalIgnoreCase)) return "hy";
            if (acceptLanguage.Contains("ru", StringComparison.OrdinalIgnoreCase)) return "ru";
            if (acceptLanguage.Contains("en", StringComparison.OrdinalIgnoreCase)) return "en";
        }

        return "en";
    }

    public async Task<IReadOnlyList<SpecialistListItemDto>> GetTopSpecialistsAsync(int count = 10, CancellationToken ct = default)
    {
        var specialists = await _db.Specialists
            .Include(sp => sp.Services)
            .Where(sp => sp.Status == "Verified")
            .OrderByDescending(sp => sp.Id)
            .Take(count)
            .ToListAsync(ct);

        var lang = GetLanguage();

        return specialists.Select(s => new SpecialistListItemDto(
            Id: s.Id,
            UserId: s.Id,
            FullName: LocalizationHelper.LocalizeString(s.FullName, lang),
            Address: LocalizationHelper.LocalizeString(s.Address, lang),
            LogoUrl: s.LogoUrl,
            Description: LocalizationHelper.LocalizeString(s.Description, lang),
            WorkingHours: s.WorkingHours,
            PreferredColors: s.PreferredColors,
            Services: s.Services
                .Select(svc => new ServiceItemDto(svc.Name, svc.Category, svc.Price, svc.DurationMinutes, svc.IsActive))
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
            .Where(s => s.Status == "Verified")
            .OrderByDescending(s => s.Id)
            .Take(count)
            .ToListAsync(ct);

        var lang = GetLanguage();
        var specIds = salons.SelectMany(s => s.StaffMembers).Where(sm => sm.SpecialistId.HasValue).Select(sm => sm.SpecialistId.Value).Distinct().ToList();
        var specialistServices = await _db.ServiceItems
            .Where(s => s.SpecialistId.HasValue && specIds.Contains(s.SpecialistId.Value))
            .ToListAsync(ct);

        return salons.Select(s => new SalonListItemDto(
            Id: s.Id,
            UserId: s.Id,
            SalonName: LocalizationHelper.LocalizeString(s.SalonName, lang),
            Address: LocalizationHelper.LocalizeString(s.Address, lang),
            LogoUrl: s.LogoUrl,
            Description: LocalizationHelper.LocalizeString(s.Description, lang),
            OperatingHours: s.OperatingHours,
            PreferredColors: s.PreferredColors,
            StaffMembers: s.StaffMembers
                .Select(sm => {
                    var smServices = sm.Services.ToList();
                    if (sm.SpecialistId.HasValue)
                    {
                        var specServices = specialistServices.Where(svc => svc.SpecialistId == sm.SpecialistId.Value).ToList();
                        smServices.AddRange(specServices);
                    }
                    return new StaffMemberDto(
                        sm.FullName,
                        sm.Title ?? string.Empty,
                        sm.GraphicsUrl,
                        smServices
                            .Select(svc => new ServiceItemDto(svc.Name, svc.Category, svc.Price, svc.DurationMinutes, svc.IsActive))
                            .ToList()
                            .AsReadOnly()
                    );
                })
                .ToList()
                .AsReadOnly(),
            AverageRating: null,
            ReviewCount: 0
        )).ToList().AsReadOnly();
    }
}
