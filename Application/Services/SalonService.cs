using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class SalonService : ISalonService
{
    private const int PageSize = 10;

    private readonly ISalonRepository _repository;
    private readonly ApplicationDbContext _context;

    public SalonService(ISalonRepository repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<PaginatedResponseDto<SalonListItemDto>> GetPagedAsync(int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var (items, totalCount) = await _repository.GetPagedAsync(page, PageSize, ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        var dtos = items.Select(s => new SalonListItemDto
        {
            Id = s.Id,
            UserId = s.Id,
            OwnerFullName = s.FullName,
            SalonName = s.SalonName,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            OperatingHours = s.OperatingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
            Rating = s.Rating,
            StartingPrice = s.StartingPrice,
            AvailabilityStatus = s.AvailabilityStatus,
            StaffMembers = s.StaffMembers.Select(sm => new StaffMemberDto
            {
                Id = sm.Id,
                FullName = sm.FullName,
                Title = sm.Title,
                GraphicsUrl = sm.GraphicsUrl,
                WorkingHours = sm.WorkingHours,
                Services = sm.Services.Select(svc => new ServiceItemDto
                {
                    Id = svc.Id,
                    Name = svc.Name,
                    Category = svc.Category,
                    Price = svc.Price,
                    DurationMinutes = svc.DurationMinutes
                }).ToList()
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

        return new SalonListItemDto
        {
            Id = salon.Id,
            UserId = salon.Id,
            OwnerFullName = salon.FullName,
            SalonName = salon.SalonName,
            Address = salon.Address,
            Latitude = salon.Latitude,
            Longitude = salon.Longitude,
            Description = salon.Description,
            LogoUrl = salon.LogoUrl,
            OperatingHours = salon.OperatingHours,
            SocialMedias = salon.SocialMedias,
            PreferredColors = salon.PreferredColors,
            Rating = salon.Rating,
            StartingPrice = salon.StartingPrice,
            AvailabilityStatus = salon.AvailabilityStatus,
            StaffMembers = salon.StaffMembers.Select(sm => new StaffMemberDto
            {
                Id = sm.Id,
                FullName = sm.FullName,
                Title = sm.Title,
                GraphicsUrl = sm.GraphicsUrl,
                WorkingHours = sm.WorkingHours,
                Services = sm.Services.Select(svc => new ServiceItemDto
                {
                    Id = svc.Id,
                    Name = svc.Name,
                    Category = svc.Category,
                    Price = svc.Price,
                    DurationMinutes = svc.DurationMinutes
                }).ToList()
            }).ToList()
        };
    }

    public async Task<List<SalonListItemDto>> GetClosestAsync(double latitude, double longitude, int limit, Guid? categoryId = null, CancellationToken ct = default)
    {
        var salons = await _context.Salons
            .Include(s => s.StaffMembers)
                .ThenInclude(sm => sm.Services)
            .ToListAsync(ct);

        if (categoryId.HasValue)
        {
            var category = await _context.ServiceCategories.FindAsync(new object[] { categoryId.Value }, ct);
            if (category != null)
            {
                var en = (category.NameEn ?? "").Trim().ToLower();
                var ru = (category.NameRu ?? "").Trim().ToLower();
                var hy = (category.NameHy ?? "").Trim().ToLower();

                salons = salons.Where(s => s.StaffMembers.Any(sm => sm.Services.Any(svc => {
                    var sc = (svc.Category ?? "").Trim().ToLower();
                    return (en != "" && (sc.Contains(en) || en.Contains(sc))) ||
                           (ru != "" && (sc.Contains(ru) || ru.Contains(sc))) ||
                           (hy != "" && (sc.Contains(hy) || hy.Contains(sc)));
                }))).ToList();
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

        return sorted.Select(s => new SalonListItemDto
        {
            Id = s.Id,
            UserId = s.Id,
            OwnerFullName = s.FullName,
            SalonName = s.SalonName,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            OperatingHours = s.OperatingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
            Rating = s.Rating,
            StartingPrice = s.StartingPrice,
            AvailabilityStatus = s.AvailabilityStatus,
            StaffMembers = s.StaffMembers.Select(sm => new StaffMemberDto
            {
                Id = sm.Id,
                FullName = sm.FullName,
                Title = sm.Title,
                GraphicsUrl = sm.GraphicsUrl,
                WorkingHours = sm.WorkingHours,
                Services = sm.Services.Select(svc => new ServiceItemDto
                {
                    Id = svc.Id,
                    Name = svc.Name,
                    Category = svc.Category,
                    Price = svc.Price,
                    DurationMinutes = svc.DurationMinutes
                }).ToList()
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
