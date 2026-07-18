using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class SalonService : ISalonService
{
    private const int PageSize = 10;

    private readonly ISalonRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SalonService(ISalonRepository repository, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _context = context;
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

    public async Task<PaginatedResponseDto<SalonListItemDto>> GetPagedAsync(int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var (items, totalCount) = await _repository.GetPagedAsync(page, PageSize, ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        var lang = GetLanguage();

        var salonsList = items.ToList();
        var specIds = salonsList.SelectMany(s => s.StaffMembers).Where(sm => sm.SpecialistId.HasValue).Select(sm => sm.SpecialistId.Value).Distinct().ToList();
        var specialistServices = await _context.ServiceItems
            .Where(s => s.SpecialistId.HasValue && specIds.Contains(s.SpecialistId.Value))
            .ToListAsync(ct);

        var dtos = salonsList.Select(s => new SalonListItemDto
        {
            Id = s.Id,
            UserId = s.Id,
            OwnerFullName = s.FullName,
            SalonName = LocalizationHelper.LocalizeString(s.SalonName, lang),
            Address = LocalizationHelper.LocalizeString(s.Address, lang),
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = LocalizationHelper.LocalizeString(s.Description, lang),
            LogoUrl = s.LogoUrl,
            OperatingHours = s.OperatingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
            Rating = s.Rating,
            StartingPrice = s.StartingPrice,
            AvailabilityStatus = s.AvailabilityStatus,
            StaffMembers = s.StaffMembers.Select(sm => {
                var smServices = sm.Services.ToList();
                if (sm.SpecialistId.HasValue)
                {
                    var specServices = specialistServices.Where(svc => svc.SpecialistId == sm.SpecialistId.Value).ToList();
                    smServices.AddRange(specServices);
                }
                return new StaffMemberDto
                {
                    Id = sm.Id,
                    FullName = sm.FullName,
                    Title = sm.Title,
                    GraphicsUrl = sm.GraphicsUrl,
                    WorkingHours = sm.WorkingHours,
                    Status = sm.Status,
                    SpecialistId = sm.SpecialistId,
                    Services = smServices.Select(svc => new ServiceItemDto
                    {
                        Id = svc.Id,
                        Name = svc.Name,
                        Category = svc.Category,
                        Price = svc.Price,
                        DurationMinutes = svc.DurationMinutes,
                        IsActive = svc.IsActive
                    }).ToList()
                };
            }).ToList()
        }).ToList();

        return new PaginatedResponseDto<SalonListItemDto>
        {
            Items = dtos,
            Page = page,
            PageSize = PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<SalonListItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var salon = await _repository.GetByIdAsync(id, ct);
        if (salon is null) return null;

        var specIds = salon.StaffMembers.Where(sm => sm.SpecialistId.HasValue).Select(sm => sm.SpecialistId.Value).ToList();
        var specialistServices = await _context.ServiceItems
            .Where(s => s.SpecialistId.HasValue && specIds.Contains(s.SpecialistId.Value))
            .ToListAsync(ct);

        var lang = GetLanguage();

        return new SalonListItemDto
        {
            Id = salon.Id,
            UserId = salon.Id,
            OwnerFullName = salon.FullName,
            SalonName = LocalizationHelper.LocalizeString(salon.SalonName, lang),
            Address = LocalizationHelper.LocalizeString(salon.Address, lang),
            Latitude = salon.Latitude,
            Longitude = salon.Longitude,
            Description = LocalizationHelper.LocalizeString(salon.Description, lang),
            LogoUrl = salon.LogoUrl,
            OperatingHours = salon.OperatingHours,
            SocialMedias = salon.SocialMedias,
            PreferredColors = salon.PreferredColors,
            Rating = salon.Rating,
            StartingPrice = salon.StartingPrice,
            AvailabilityStatus = salon.AvailabilityStatus,
            StaffMembers = salon.StaffMembers.Select(sm => {
                var smServices = sm.Services.ToList();
                if (sm.SpecialistId.HasValue)
                {
                    var specServices = specialistServices.Where(svc => svc.SpecialistId == sm.SpecialistId.Value).ToList();
                    smServices.AddRange(specServices);
                }
                return new StaffMemberDto
                {
                    Id = sm.Id,
                    FullName = sm.FullName,
                    Title = sm.Title,
                    GraphicsUrl = sm.GraphicsUrl,
                    WorkingHours = sm.WorkingHours,
                    Status = sm.Status,
                    SpecialistId = sm.SpecialistId,
                    Services = smServices.Select(svc => new ServiceItemDto
                    {
                        Id = svc.Id,
                        Name = svc.Name,
                        Category = svc.Category,
                        Price = svc.Price,
                        DurationMinutes = svc.DurationMinutes,
                        IsActive = svc.IsActive
                    }).ToList()
                };
            }).ToList()
        };
    }

    public async Task<List<SalonListItemDto>> GetClosestAsync(double latitude, double longitude, int limit, Guid? categoryId = null, CancellationToken ct = default)
    {
        var salons = await _context.Salons
            .Include(s => s.StaffMembers)
                .ThenInclude(sm => sm.Services)
            .Where(s => s.Status == "Verified")
            .ToListAsync(ct);

        // Preload linked specialist services for ALL staff members in these salons
        var allSpecIds = salons.SelectMany(s => s.StaffMembers)
                               .Where(sm => sm.SpecialistId.HasValue)
                               .Select(sm => sm.SpecialistId!.Value)
                               .Distinct()
                               .ToList();
                               
        var specialistServices = await _context.ServiceItems
            .Where(s => s.SpecialistId.HasValue && allSpecIds.Contains(s.SpecialistId.Value))
            .ToListAsync(ct);

        if (categoryId.HasValue)
        {
            var category = await _context.ServiceCategories.FindAsync(new object[] { categoryId.Value }, ct);
            if (category != null)
            {
                var en = (category.NameEn ?? "").Trim().ToLower();
                var ru = (category.NameRu ?? "").Trim().ToLower();
                var hy = (category.NameHy ?? "").Trim().ToLower();

                salons = salons.Where(s => s.StaffMembers.Any(sm => {
                    var smServices = sm.Services.Select(svc => svc.Category).ToList();
                    if (sm.SpecialistId.HasValue)
                    {
                        var specSvcCategories = specialistServices
                            .Where(svc => svc.SpecialistId == sm.SpecialistId.Value)
                            .Select(svc => svc.Category)
                            .ToList();
                        smServices.AddRange(specSvcCategories);
                    }

                    return smServices.Any(cat => {
                        var sc = (cat ?? "").Trim().ToLower();
                        return (en != "" && (sc.Contains(en) || en.Contains(sc))) ||
                               (ru != "" && (sc.Contains(ru) || ru.Contains(sc))) ||
                               (hy != "" && (sc.Contains(hy) || hy.Contains(sc)));
                    });
                })).ToList();
            }
        }

        var sorted = salons
            .Select(s => new
            {
                Salon = s,
                Distance = GetDistance(latitude, longitude, s.Latitude, s.Longitude)
            })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .Select(x => x.Salon)
            .ToList();

        var lang = GetLanguage();

        return sorted.Select(s => new SalonListItemDto
        {
            Id = s.Id,
            UserId = s.Id,
            OwnerFullName = s.FullName,
            SalonName = LocalizationHelper.LocalizeString(s.SalonName, lang),
            Address = LocalizationHelper.LocalizeString(s.Address, lang),
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = LocalizationHelper.LocalizeString(s.Description, lang),
            LogoUrl = s.LogoUrl,
            OperatingHours = s.OperatingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
            Rating = s.Rating,
            StartingPrice = s.StartingPrice,
            AvailabilityStatus = s.AvailabilityStatus,
            StaffMembers = s.StaffMembers.Select(sm => {
                var smServices = sm.Services.ToList();
                if (sm.SpecialistId.HasValue)
                {
                    var specServices = specialistServices.Where(svc => svc.SpecialistId == sm.SpecialistId.Value).ToList();
                    smServices.AddRange(specServices);
                }
                return new StaffMemberDto
                {
                    Id = sm.Id,
                    FullName = sm.FullName,
                    Title = sm.Title,
                    GraphicsUrl = sm.GraphicsUrl,
                    WorkingHours = sm.WorkingHours,
                    Status = sm.Status,
                    SpecialistId = sm.SpecialistId,
                    Services = smServices.Select(svc => new ServiceItemDto
                    {
                        Id = svc.Id,
                        Name = svc.Name,
                        Category = svc.Category,
                        Price = svc.Price,
                        DurationMinutes = svc.DurationMinutes,
                        IsActive = svc.IsActive
                    }).ToList()
                };
            }).ToList()
        }).ToList();
    }

    private static double GetDistance(double lat1, double lon1, double? lat2, double? lon2)
    {
        if (!lat2.HasValue || !lon2.HasValue) return double.MaxValue;
        var r = 6371; // Earth radius in km
        var dLat = ToRadians(lat2.Value - lat1);
        var dLon = ToRadians(lon2.Value - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2.Value)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double ToRadians(double val)
    {
        return (Math.PI / 180) * val;
    }
}
