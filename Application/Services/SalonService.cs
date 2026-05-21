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

        // Batch-fetch owner names for all salons in one query
        var userIds = items.Select(s => s.UserId).Distinct().ToList();
        var userNames = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        var dtos = items.Select(s => new SalonListItemDto
        {
            Id = s.Id,
            UserId = s.UserId,
            OwnerFullName = userNames.TryGetValue(s.UserId, out var name) ? name : string.Empty,
            SalonName = s.SalonName,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            OperatingHours = s.OperatingHours,
            SocialMedias = s.SocialMedias,
            PreferredColors = s.PreferredColors,
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

        var user = await _context.Users
            .Where(u => u.Id == salon.UserId)
            .Select(u => new { u.FullName })
            .FirstOrDefaultAsync(ct);

        return new SalonListItemDto
        {
            Id = salon.Id,
            UserId = salon.UserId,
            OwnerFullName = user?.FullName ?? string.Empty,
            SalonName = salon.SalonName,
            Address = salon.Address,
            Latitude = salon.Latitude,
            Longitude = salon.Longitude,
            Description = salon.Description,
            LogoUrl = salon.LogoUrl,
            OperatingHours = salon.OperatingHours,
            SocialMedias = salon.SocialMedias,
            PreferredColors = salon.PreferredColors,
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
}
