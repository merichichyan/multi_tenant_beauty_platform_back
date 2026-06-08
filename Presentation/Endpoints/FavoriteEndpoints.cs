using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class FavoriteEndpoints
{
    public static IEndpointRouteBuilder MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/favorites")
                       .WithTags("Favorites")
                       .RequireAuthorization();

        group.MapGet("/ids", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var salonIds = await context.FavoriteSalons
                .Where(f => f.UserId == userId)
                .Select(f => f.SalonId.ToString())
                .ToListAsync(ct);

            var specialistIds = await context.FavoriteSpecialists
                .Where(f => f.UserId == userId)
                .Select(f => f.SpecialistId.ToString())
                .ToListAsync(ct);

            return Results.Ok(new { salonIds, specialistIds });
        })
        .WithSummary("Get list of favorited salon and specialist IDs for current user");

        group.MapGet("/salons", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var favoriteSalonIds = await context.FavoriteSalons
                .Where(f => f.UserId == userId)
                .Select(f => f.SalonId)
                .ToListAsync(ct);

            var salons = await context.Salons
                .Include(s => s.StaffMembers)
                    .ThenInclude(sm => sm.Services)
                .Where(s => favoriteSalonIds.Contains(s.Id) && (s.Status == "Verified" || s.Status == "Approved"))
                .ToListAsync(ct);

            var specIds = salons.SelectMany(s => s.StaffMembers).Where(sm => sm.SpecialistId.HasValue).Select(sm => sm.SpecialistId.Value).Distinct().ToList();
            var specialistServices = await context.ServiceItems
                .Where(s => s.SpecialistId.HasValue && specIds.Contains(s.SpecialistId.Value))
                .ToListAsync(ct);

            var dtos = salons.Select(s => new SalonListItemDto
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

            return Results.Ok(dtos);
        })
        .WithSummary("Get user's favorite salons");

        group.MapGet("/specialists", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var favoriteSpecialistIds = await context.FavoriteSpecialists
                .Where(f => f.UserId == userId)
                .Select(f => f.SpecialistId)
                .ToListAsync(ct);

            var specialists = await context.Specialists
                .Include(s => s.Services)
                .Where(s => favoriteSpecialistIds.Contains(s.Id) && (s.Status == "Verified" || s.Status == "Approved"))
                .ToListAsync(ct);

            var dtos = specialists.Select(s => new SpecialistListItemDto
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
                    DurationMinutes = svc.DurationMinutes,
                    IsActive = svc.IsActive
                }).ToList()
            }).ToList();

            return Results.Ok(dtos);
        })
        .WithSummary("Get user's favorite specialists");

        group.MapPost("/salons/{salonId:guid}/toggle", async (Guid salonId, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var salonExists = await context.Salons.AnyAsync(s => s.Id == salonId && (s.Status == "Verified" || s.Status == "Approved"), ct);
            if (!salonExists)
            {
                return Results.NotFound(new { message = "Salon not found or not verified" });
            }

            var existing = await context.FavoriteSalons
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SalonId == salonId, ct);

            bool isFavorite;
            if (existing != null)
            {
                context.FavoriteSalons.Remove(existing);
                isFavorite = false;
            }
            else
            {
                var favorite = new FavoriteSalon(userId, salonId);
                context.FavoriteSalons.Add(favorite);
                isFavorite = true;
            }

            await context.SaveChangesAsync(ct);
            return Results.Ok(new { isFavorite });
        })
        .WithSummary("Toggle salon favorite status");

        group.MapPost("/specialists/{specialistId:guid}/toggle", async (Guid specialistId, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var specialistExists = await context.Specialists.AnyAsync(s => s.Id == specialistId && (s.Status == "Verified" || s.Status == "Approved"), ct);
            if (!specialistExists)
            {
                return Results.NotFound(new { message = "Specialist not found or not verified" });
            }

            var existing = await context.FavoriteSpecialists
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SpecialistId == specialistId, ct);

            bool isFavorite;
            if (existing != null)
            {
                context.FavoriteSpecialists.Remove(existing);
                isFavorite = false;
            }
            else
            {
                var favorite = new FavoriteSpecialist(userId, specialistId);
                context.FavoriteSpecialists.Add(favorite);
                isFavorite = true;
            }

            await context.SaveChangesAsync(ct);
            return Results.Ok(new { isFavorite });
        })
        .WithSummary("Toggle specialist favorite status");

        return app;
    }
}
