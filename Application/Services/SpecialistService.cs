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

        // Batch-fetch owner names for all specialists in one query
        var userIds = items.Select(s => s.UserId).Distinct().ToList();
        var userNames = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        var dtos = items.Select(s => new SpecialistListItemDto
        {
            Id = s.Id,
            UserId = s.UserId,
            FullName = userNames.TryGetValue(s.UserId, out var name) ? name : string.Empty,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            WorkingHours = s.WorkingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
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

        var user = await _context.Users
            .Where(u => u.Id == specialist.UserId)
            .Select(u => new { u.FullName })
            .FirstOrDefaultAsync(ct);

        return new SpecialistListItemDto
        {
            Id = specialist.Id,
            UserId = specialist.UserId,
            FullName = user?.FullName ?? string.Empty,
            Address = specialist.Address,
            Latitude = specialist.Latitude,
            Longitude = specialist.Longitude,
            Description = specialist.Description,
            LogoUrl = specialist.LogoUrl,
            WorkingHours = specialist.WorkingHours,
            SocialMedias = specialist.SocialMedias,
            PreferredColors = specialist.PreferredColors,
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
}
