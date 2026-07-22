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
                       .WithTags("Salons");

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
            var linkedSpecialistIds = salon.StaffMembers
                                           .Where(sm => sm.SpecialistId.HasValue)
                                           .Select(sm => sm.SpecialistId!.Value)
                                           .ToList();

            var bookings = await context.Bookings
                                        .Where(b => staffIds.Contains(b.SpecialistId) || (b.SpecialistId != Guid.Empty && linkedSpecialistIds.Contains(b.SpecialistId)))
                                        .ToListAsync(ct);

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var now = DateTime.Now;
            bool IsCompleted(Booking b)
            {
                if (b.IsNoShow) return false;
                try
                {
                    var slotParts = b.TimeSlot.Split('-');
                    var endPart = slotParts[1].Trim();
                    
                    var isAmPm = endPart.ToLower().Contains("am") || endPart.ToLower().Contains("pm");
                    int hour = 0, minute = 0;
                    if (isAmPm)
                    {
                        var parts = endPart.Split(' ');
                        var hm = parts[0].Split(':');
                        hour = int.Parse(hm[0]);
                        minute = int.Parse(hm[1]);
                        if (parts[1].ToLower() == "pm" && hour < 12)
                        {
                            hour += 12;
                        }
                        else if (parts[1].ToLower() == "am" && hour == 12)
                        {
                            hour = 0;
                        }
                    }
                    else
                    {
                        var hm = endPart.Split(':');
                        hour = int.Parse(hm[0]);
                        minute = int.Parse(hm[1]);
                        if (hour < 8)
                        {
                            hour += 12;
                        }
                    }

                    var endDateTime = b.BookingDate.Date.AddHours(hour).AddMinutes(minute);
                    return now > endDateTime;
                }
                catch
                {
                    return b.BookingDate.Date < now.Date;
                }
            }

            var bookingsThisMonth = bookings.Count(b => b.BookingDate.Date >= startOfMonth && b.BookingDate.Date <= today);
            var bookingsToday = bookings.Count(b => b.BookingDate.Date == today);

            var incomeThisMonth = bookings.Where(b => b.BookingDate.Date >= startOfMonth && b.BookingDate.Date <= today && IsCompleted(b))
                                           .Sum(b => b.Price);
            var incomeToday = bookings.Where(b => b.BookingDate.Date == today && IsCompleted(b))
                                       .Sum(b => b.Price);

            var totalStaffCount = salon.StaffMembers.Count;
            var presentTodayCount = salon.StaffMembers.Count(sm => sm.Status != "Off Duty");

            // Look up user names for today's bookings
            var userIds = bookings.Select(b => b.UserId).Distinct().ToList();
            var users = await context.Users
                                     .Where(u => userIds.Contains(u.Id))
                                     .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

            var todayBookingsList = bookings
                .Where(b => b.BookingDate.Date == today)
                .OrderBy(b => b.TimeSlot)
                .Select(b => new
                {
                    b.Id,
                    b.SpecialistId,
                    b.SpecialistName,
                    b.ServiceName,
                    b.Price,
                    b.DurationMinutes,
                    b.BookingDate,
                    b.TimeSlot,
                    b.UserId,
                    b.UserEmail,
                    b.CreatedAt,
                    b.IsNoShow,
                    b.SalonId,
                    b.SalonName,
                    UserName = users.TryGetValue(b.UserId, out var name) ? name : b.UserEmail
                })
                .ToList();

            return Results.Ok(new
            {
                bookingsThisMonth,
                bookingsToday,
                incomeThisMonth,
                incomeToday,
                presentTodayCount,
                totalStaffCount,
                todayBookings = todayBookingsList,
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
        .RequireAuthorization()
        .WithSummary("Get salon dashboard statistics");

        group.MapGet("/bookings", async (
            [FromQuery] Guid? staffId,
            [FromQuery] DateTime? date,
            ClaimsPrincipal principal,
            ApplicationDbContext context,
            CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var salonId))
            {
                return Results.Unauthorized();
            }

            var salon = await context.Salons
                                     .Include(s => s.StaffMembers)
                                     .FirstOrDefaultAsync(s => s.Id == salonId, ct);

            if (salon == null)
            {
                return Results.NotFound(new { message = "Salon not found" });
            }

            var staffIds = salon.StaffMembers.Select(sm => sm.Id).ToList();
            var linkedSpecialistIds = salon.StaffMembers
                                           .Where(sm => sm.SpecialistId.HasValue)
                                           .Select(sm => sm.SpecialistId!.Value)
                                           .ToList();

            var query = context.Bookings.Where(b => b.SalonId == salonId || staffIds.Contains(b.SpecialistId) || (b.SpecialistId != Guid.Empty && linkedSpecialistIds.Contains(b.SpecialistId)));

            if (staffId.HasValue)
            {
                var staff = await context.StaffMembers.FirstOrDefaultAsync(sm => sm.Id == staffId.Value && sm.SalonId == salonId, ct);
                if (staff != null)
                {
                    if (staff.SpecialistId.HasValue)
                    {
                        var specId = staff.SpecialistId.Value;
                        query = query.Where(b => b.SpecialistId == staff.Id || b.SpecialistId == specId);
                    }
                    else
                    {
                        query = query.Where(b => b.SpecialistId == staff.Id);
                    }
                }
                else
                {
                    return Results.Ok(new List<object>());
                }
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                query = query.Where(b => b.BookingDate.Date == targetDate);
            }

            var bookings = await query.OrderBy(b => b.BookingDate)
                                      .ThenBy(b => b.TimeSlot)
                                      .ToListAsync(ct);

            var userIds = bookings.Select(b => b.UserId).Distinct().ToList();
            var users = await context.Users
                                     .Where(u => userIds.Contains(u.Id))
                                     .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

            var result = bookings.Select(b => new
            {
                b.Id,
                b.SpecialistId,
                b.SpecialistName,
                b.ServiceName,
                b.Price,
                b.DurationMinutes,
                b.BookingDate,
                b.TimeSlot,
                b.UserId,
                b.UserEmail,
                b.CreatedAt,
                b.IsNoShow,
                b.SalonId,
                b.SalonName,
                UserName = users.TryGetValue(b.UserId, out var name) ? name : b.UserEmail
            });

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithSummary("Get bookings for the authenticated salon with optional filters by staff member and date");

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
        .RequireAuthorization()
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
        .RequireAuthorization()
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

            var oldSalonId = specialist.SalonId;
            specialist.UpdateSpecialistProfile(
                specialist.Address ?? string.Empty,
                specialist.Latitude,
                specialist.Longitude,
                specialist.Description,
                specialist.SocialMedias,
                specialist.LogoUrl,
                specialist.PreferredColors,
                specialist.WorkingHours,
                userId
            );

            if (oldSalonId.HasValue && oldSalonId.Value != userId)
            {
                var oldStaff = await context.StaffMembers.FirstOrDefaultAsync(sm => sm.SpecialistId == specialist.Id && sm.SalonId == oldSalonId.Value, ct);
                if (oldStaff != null) context.StaffMembers.Remove(oldStaff);
            }

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
        .RequireAuthorization()
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
        .RequireAuthorization()
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
        .RequireAuthorization()
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
        .RequireAuthorization()
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
