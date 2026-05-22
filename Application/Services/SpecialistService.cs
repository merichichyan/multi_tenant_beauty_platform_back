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
