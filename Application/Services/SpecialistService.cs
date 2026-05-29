using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class SpecialistService : ISpecialistService
{
    private const int PageSize = 10;

    private readonly ISpecialistRepository _repository;
    private readonly ApplicationDbContext _context;

    public SpecialistService(ISpecialistRepository repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<PaginatedResponseDto<SpecialistListItemDto>> GetPagedAsync(int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var (items, totalCount) = await _repository.GetPagedAsync(page, PageSize, ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        var dtos = items.Select(s => new SpecialistListItemDto
        {
            Id = s.Id,
            UserId = s.Id,
            FullName = s.FullName,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            WorkingHours = s.WorkingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
            Rating = s.Rating,
            StartingPrice = s.StartingPrice,
            AvailabilityStatus = s.AvailabilityStatus,
            Services = s.Services.Select(svc => new ServiceItemDto
            {
                Id = svc.Id,
                Name = svc.Name,
                Category = svc.Category,
                Price = svc.Price,
                DurationMinutes = svc.DurationMinutes
            }).ToList()
        }).ToList();

        return new PaginatedResponseDto<SpecialistListItemDto>
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

    public async Task<SpecialistListItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var specialist = await _repository.GetByIdAsync(id, ct);
        if (specialist is null) return null;

        return new SpecialistListItemDto
        {
            Id = specialist.Id,
            UserId = specialist.Id,
            FullName = specialist.FullName,
            Address = specialist.Address,
            Latitude = specialist.Latitude,
            Longitude = specialist.Longitude,
            Description = specialist.Description,
            LogoUrl = specialist.LogoUrl,
            WorkingHours = specialist.WorkingHours,
            SocialMedias = specialist.SocialMedias,
            PreferredColors = specialist.PreferredColors,
            Rating = specialist.Rating,
            StartingPrice = specialist.StartingPrice,
            AvailabilityStatus = specialist.AvailabilityStatus,
            Services = specialist.Services.Select(svc => new ServiceItemDto
            {
                Id = svc.Id,
                Name = svc.Name,
                Category = svc.Category,
                Price = svc.Price,
                DurationMinutes = svc.DurationMinutes
            }).ToList()
        };
    }

    public async Task<List<SpecialistListItemDto>> GetFeaturedAsync(CancellationToken ct = default)
    {
        var specialists = await _context.Specialists
            .Include(s => s.Services)
            .Where(s => s.Status == "Verified")
            .OrderByDescending(s => s.Rating)
            .Take(5)
            .ToListAsync(ct);

        return specialists.Select(s => new SpecialistListItemDto
        {
            Id = s.Id,
            UserId = s.Id,
            FullName = s.FullName,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            WorkingHours = s.WorkingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
            Rating = s.Rating,
            StartingPrice = s.StartingPrice,
            AvailabilityStatus = s.AvailabilityStatus,
            Services = s.Services.Select(svc => new ServiceItemDto
            {
                Id = svc.Id,
                Name = svc.Name,
                Category = svc.Category,
                Price = svc.Price,
                DurationMinutes = svc.DurationMinutes
            }).ToList()
        }).ToList();
    }

    public async Task<List<SpecialistListItemDto>> GetClosestAsync(double latitude, double longitude, int limit, Guid? categoryId = null, CancellationToken ct = default)
    {
        var specialists = await _context.Specialists
            .Include(s => s.Services)
            .Where(s => s.Status == "Verified")
            .ToListAsync(ct);

        if (categoryId.HasValue)
        {
            var category = await _context.ServiceCategories.FindAsync(new object[] { categoryId.Value }, ct);
            if (category != null)
            {
                var en = (category.NameEn ?? "").Trim().ToLower();
                var ru = (category.NameRu ?? "").Trim().ToLower();
                var hy = (category.NameHy ?? "").Trim().ToLower();

                specialists = specialists.Where(spec => spec.Services.Any(svc => {
                    var sc = (svc.Category ?? "").Trim().ToLower();
                    return (en != "" && (sc.Contains(en) || en.Contains(sc))) ||
                           (ru != "" && (sc.Contains(ru) || ru.Contains(sc))) ||
                           (hy != "" && (sc.Contains(hy) || hy.Contains(sc)));
                })).ToList();
            }
        }

        var sorted = specialists
            .Select(s => new
            {
                Specialist = s,
                Distance = GetDistance(latitude, longitude, s.Latitude, s.Longitude)
            })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .Select(x => x.Specialist)
            .ToList();

        return sorted.Select(s => new SpecialistListItemDto
        {
            Id = s.Id,
            UserId = s.Id,
            FullName = s.FullName,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            WorkingHours = s.WorkingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
            Rating = s.Rating,
            StartingPrice = s.StartingPrice,
            AvailabilityStatus = s.AvailabilityStatus,
            Services = s.Services.Select(svc => new ServiceItemDto
            {
                Id = svc.Id,
                Name = svc.Name,
                Category = svc.Category,
                Price = svc.Price,
                DurationMinutes = svc.DurationMinutes
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
