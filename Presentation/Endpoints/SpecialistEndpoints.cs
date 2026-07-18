using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class SpecialistEndpoints
{
    public static IEndpointRouteBuilder MapSpecialistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/specialists")
                       .WithTags("Specialists");

        group.MapGet("/", async ([FromQuery] int page, [FromQuery] string? query, ISpecialistService service, CancellationToken ct) =>
        {
            if (page < 1) page = 1;
            var result = await service.GetPagedAsync(page, query, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get paginated list of specialists (10 per page)")
        .WithDescription("Returns specialists with their services. Pass ?page=1, ?page=2, etc.");

        group.MapGet("/featured", async (ISpecialistService service, CancellationToken ct) =>
        {
            var result = await service.GetFeaturedAsync(ct);
            return Results.Ok(result);
        })
        .WithSummary("Get featured specialists sorted by rating");

        group.MapGet("/closest", async (double? latitude, double? longitude, int? limit, Guid? categoryId, ISpecialistService service, CancellationToken ct) =>
        {
            double lat = latitude ?? 40.1792;
            double lon = longitude ?? 44.5152;
            int lim = limit ?? 3;
            var result = await service.GetClosestAsync(lat, lon, lim, categoryId, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get closest specialists")
        .WithDescription("Returns closest specialists using distance from latitude and longitude. Defaults to Yerevan (40.1792, 44.5152).");

        group.MapGet("/{id:guid}", async (Guid id, ISpecialistService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("Get a specialist by ID");

        group.MapPost("/services", async ([FromBody] CreateServiceRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var specialist = await context.Specialists.Include(s => s.Services).FirstOrDefaultAsync(s => s.Id == userId, ct);
            if (specialist == null)
            {
                return Results.NotFound(new { message = "Specialist not found" });
            }

            var service = new ServiceItem(request.Name, request.Category, request.Price, request.DurationMinutes, specialistId: specialist.Id);
            specialist.AddService(service);
            context.ServiceItems.Add(service);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                message = "Service added successfully",
                service = new
                {
                    id = service.Id,
                    name = service.Name,
                    category = service.Category,
                    price = service.Price,
                    durationMinutes = service.DurationMinutes
                }
            });
        })
        .RequireAuthorization()
        .WithSummary("Add service to specialist");

        group.MapPatch("/services/{serviceId:guid}/toggle", async (Guid serviceId, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var service = await context.ServiceItems.FirstOrDefaultAsync(s => s.Id == serviceId && s.SpecialistId == userId, ct);
            if (service == null)
            {
                return Results.NotFound(new { message = "Service not found or unauthorized" });
            }

            service.SetActive(!service.IsActive);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                message = "Service status toggled successfully",
                isActive = service.IsActive
            });
        })
        .RequireAuthorization()
        .WithSummary("Toggle service active status");

        group.MapDelete("/services/{serviceId:guid}", async (Guid serviceId, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var service = await context.ServiceItems.FirstOrDefaultAsync(s => s.Id == serviceId && s.SpecialistId == userId, ct);
            if (service == null)
            {
                return Results.NotFound(new { message = "Service not found or unauthorized" });
            }

            context.ServiceItems.Remove(service);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Service deleted successfully" });
        })
        .RequireAuthorization()
        .WithSummary("Delete service from specialist");

        return app;
    }
}

public record CreateServiceRequest(
    string Name,
    string Category,
    decimal Price,
    int DurationMinutes
);
