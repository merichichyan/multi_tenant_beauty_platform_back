using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings")
                       .WithTags("Bookings")
                       .RequireAuthorization();

        group.MapPost("/", async ([FromBody] CreateBookingRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var emailClaim = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name ?? "user@beautyplatform.com";
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            // Find specialist or staff member details to store
            string specialistName = string.Empty;
            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == request.SpecialistId, ct);
            if (specialist != null)
            {
                specialistName = specialist.FullName;
            }
            else
            {
                var staff = await context.StaffMembers.FirstOrDefaultAsync(sm => sm.Id == request.SpecialistId, ct);
                if (staff != null)
                {
                    specialistName = staff.FullName;
                }
                else
                {
                    return Results.NotFound(new { message = "Specialist or Staff Member not found" });
                }
            }

            var booking = new Booking(
                request.SpecialistId,
                specialistName,
                request.ServiceName,
                request.Price,
                request.DurationMinutes,
                request.BookingDate,
                request.TimeSlot,
                userId,
                emailClaim
            );

            context.Bookings.Add(booking);
            await context.SaveChangesAsync(ct);

            return Results.Created($"/api/bookings/{booking.Id}", booking);
        })
        .WithSummary("Create a new booking");

        group.MapGet("/", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var bookings = await context.Bookings
                                        .Where(b => b.UserId == userId)
                                        .OrderByDescending(b => b.BookingDate)
                                        .ToListAsync(ct);

            var specialistIds = bookings.Select(b => b.SpecialistId).Distinct().ToList();
            var activeSpecialistIds = await context.Specialists
                                                   .Where(s => specialistIds.Contains(s.Id))
                                                   .Select(s => s.Id)
                                                   .ToListAsync(ct);

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
                IsSpecialistDeleted = !activeSpecialistIds.Contains(b.SpecialistId)
            });

            return Results.Ok(result);
        })
        .WithSummary("Get user bookings");

        group.MapGet("/specialist/{specialistId:guid}", async (Guid specialistId, ApplicationDbContext context, CancellationToken ct) =>
        {
            var bookings = await context.Bookings
                                        .Where(b => b.SpecialistId == specialistId)
                                        .ToListAsync(ct);

            // Look up user names for each booking
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
                UserName = users.TryGetValue(b.UserId, out var name) ? name : b.UserEmail
            });

            return Results.Ok(result);
        })
        .WithSummary("Get specialist bookings");

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (booking == null)
            {
                return Results.NotFound(new { message = "Booking not found" });
            }

            if (booking.UserId != userId)
            {
                return Results.Forbid();
            }

            try
            {
                var parts = booking.TimeSlot.Split('-');
                var startPart = parts[0].Trim();
                var timeParts = startPart.Split(':');
                var hour = int.Parse(timeParts[0]);
                var minute = int.Parse(timeParts[1]);

                var bookingStart = booking.BookingDate.Date.AddHours(hour).AddMinutes(minute);
                
                if (bookingStart - DateTime.Now < TimeSpan.FromHours(4))
                {
                    return Results.BadRequest(new { message = "Cannot cancel a booking less than 4 hours before the scheduled time." });
                }
            }
            catch (Exception)
            {
                if (booking.BookingDate.Date <= DateTime.Now.Date)
                {
                    return Results.BadRequest(new { message = "Cannot cancel a booking scheduled for today or in the past." });
                }
            }

            context.Bookings.Remove(booking);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Booking cancelled successfully" });
        })
        .WithSummary("Cancel a booking");

        group.MapPatch("/{id:guid}/no-show", async (Guid id, [FromQuery] bool isNoShow, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (booking == null)
            {
                return Results.NotFound(new { message = "Booking not found" });
            }

            // Verify that the current user is a specialist and is the specialist for this booking
            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == userId, ct);
            if (specialist == null || booking.SpecialistId != specialist.Id)
            {
                return Results.Forbid();
            }

            booking.MarkAsNoShow(isNoShow);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Booking no-show status updated successfully", isNoShow = booking.IsNoShow });
        })
        .WithSummary("Toggle no-show status for a booking");

        return app;
    }
}

public record CreateBookingRequest(
    Guid SpecialistId,
    string ServiceName,
    decimal Price,
    int DurationMinutes,
    DateTime BookingDate,
    string TimeSlot
);
