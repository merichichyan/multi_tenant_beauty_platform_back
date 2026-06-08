using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class SalonEndpoints
{
    public static IEndpointRouteBuilder MapSalonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/salons")
                       .WithTags("Salons")
                       .RequireAuthorization();

        group.MapGet("/dashboard", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var salon = await context.Salons
                                     .Include(s => s.StaffMembers)
                                     .ThenInclude(sm => sm.Services)
                                     .FirstOrDefaultAsync(s => s.Id == userId, ct);

            if (salon == null)
            {
                return Results.NotFound(new { message = "Salon not found" });
            }

            var staffIds = salon.StaffMembers.Select(sm => sm.Id).ToList();

            var bookings = await context.Bookings
                                        .Where(b => staffIds.Contains(b.SpecialistId))
                                        .ToListAsync(ct);

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // In-memory calculations
            var bookingsThisMonth = bookings.Count(b => b.BookingDate.Date >= startOfMonth && b.BookingDate.Date <= today);
            var bookingsToday = bookings.Count(b => b.BookingDate.Date == today);

            var incomeThisMonth = bookings.Where(b => b.BookingDate.Date >= startOfMonth && b.BookingDate.Date <= today)
                                           .Sum(b => b.Price);
            var incomeToday = bookings.Where(b => b.BookingDate.Date == today)
                                       .Sum(b => b.Price);

            var totalStaffCount = salon.StaffMembers.Count;
            var presentTodayCount = salon.StaffMembers.Count(sm => sm.Status != "Off Duty");

            return Results.Ok(new
            {
                bookingsThisMonth,
                bookingsToday,
                incomeThisMonth,
                incomeToday,
                presentTodayCount,
                totalStaffCount,
                staffMembers = salon.StaffMembers.Select(sm => new
                {
                    id = sm.Id,
                    fullName = sm.FullName,
                    title = sm.Title,
                    graphicsUrl = sm.GraphicsUrl,
                    workingHours = sm.WorkingHours,
                    status = sm.Status,
                    services = sm.Services.Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        category = s.Category,
                        price = s.Price,
                        durationMinutes = s.DurationMinutes,
                        isActive = s.IsActive
                    }).ToList()
                }).ToList()
            });
        })
        .WithSummary("Get salon dashboard statistics");

        group.MapPatch("/staff/{staffId:guid}/status", async (Guid staffId, [FromBody] UpdateStaffStatusRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var staff = await context.StaffMembers.FirstOrDefaultAsync(sm => sm.Id == staffId, ct);
            if (staff == null)
            {
                return Results.NotFound(new { message = "Staff member not found" });
            }

            if (staff.SalonId != userId)
            {
                return Results.Forbid();
            }

            staff.UpdateStatus(request.Status);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Staff status updated successfully", status = staff.Status });
        })
        .WithSummary("Update a staff member's status");

        group.MapGet("/", async (int page, ISalonService service, CancellationToken ct) =>
        {
            if (page < 1) page = 1;
            var result = await service.GetPagedAsync(page, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get paginated list of salons (10 per page)");

        group.MapGet("/closest", async (double? latitude, double? longitude, int? limit, Guid? categoryId, ISalonService service, CancellationToken ct) =>
        {
            double lat = latitude ?? 40.1792;
            double lon = longitude ?? 44.5152;
            int lim = limit ?? 3;
            var result = await service.GetClosestAsync(lat, lon, lim, categoryId, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get closest salons")
        .WithDescription("Returns closest salons using distance from latitude and longitude. Defaults to Yerevan (40.1792, 44.5152).");

        group.MapGet("/{id:guid}", async (Guid id, ISalonService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("Get a salon by ID");

        return app;
    }
}

public record UpdateStaffStatusRequest(string Status);
