using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Domain.Entities;
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

        group.MapGet("/staff", async ([FromQuery] int pageNumber, [FromQuery] int pageSize, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = context.StaffMembers
                               .Where(sm => sm.SalonId == userId)
                               .OrderBy(sm => sm.FullName);

            var totalCount = await query.CountAsync(ct);
            var items = await query.Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .Select(sm => new
                                   {
                                       id = sm.Id,
                                       fullName = sm.FullName,
                                       title = sm.Title,
                                       graphicsUrl = sm.GraphicsUrl,
                                       workingHours = sm.WorkingHours,
                                       status = sm.Status,
                                       specialistId = sm.SpecialistId
                                   })
                                   .ToListAsync(ct);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Results.Ok(new
            {
                items,
                page = pageNumber,
                pageSize,
                totalCount,
                totalPages,
                hasNextPage = pageNumber < totalPages,
                hasPreviousPage = pageNumber > 1
            });
        })
        .WithSummary("Get paginated list of staff members for salon");

        group.MapPost("/staff", async ([FromBody] CreateStaffRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            if (!request.SpecialistId.HasValue)
            {
                return Results.BadRequest(new { message = "SpecialistId is required to link an existing specialist." });
            }

            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == request.SpecialistId.Value, ct);
            if (specialist == null)
            {
                return Results.NotFound(new { message = "Specialist to link not found." });
            }

            string fullName = specialist.FullName;
            string? title = specialist.Description ?? "Specialist";
            string? graphicsUrl = specialist.LogoUrl;
            string? workingHours = specialist.WorkingHours;

            var staff = new StaffMember(userId, fullName, title, graphicsUrl, workingHours, "Active", request.SpecialistId);
            context.StaffMembers.Add(staff);
            await context.SaveChangesAsync(ct);

            return Results.Created($"/api/salons/staff/{staff.Id}", new
            {
                id = staff.Id,
                fullName = staff.FullName,
                title = staff.Title,
                graphicsUrl = staff.GraphicsUrl,
                workingHours = staff.WorkingHours,
                status = staff.Status,
                specialistId = staff.SpecialistId
            });
        })
        .WithSummary("Add a new staff member or link an existing specialist");

        group.MapPut("/staff/{staffId:guid}", async (Guid staffId, [FromBody] UpdateStaffRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
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

            staff.UpdateWorkingHours(request.WorkingHours);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Staff member updated successfully" });
        })
        .WithSummary("Update a staff member's details");

        group.MapDelete("/staff/{staffId:guid}", async (Guid staffId, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
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

            staff.UpdateStatus("Inactive");
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Staff member deactivated successfully" });
        })
        .WithSummary("Deactivate (soft delete) a staff member");

        group.MapPost("/staff/{staffId:guid}/activate", async (Guid staffId, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
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

            staff.UpdateStatus("Active");
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Staff member activated successfully" });
        })
        .WithSummary("Activate a deactivated staff member");

        return app;
    }
}

public record UpdateStaffStatusRequest(string Status);

public record CreateStaffRequest(
    string FullName,
    string? Title,
    string? GraphicsUrl,
    string? WorkingHours,
    Guid? SpecialistId
);

public record UpdateStaffRequest(
    string? WorkingHours
);
