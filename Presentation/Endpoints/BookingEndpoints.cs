using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Services;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings")
                       .WithTags("Bookings")
                       .RequireAuthorization();

        group.MapPost("/", async ([FromBody] CreateBookingRequest request, ClaimsPrincipal principal, ApplicationDbContext context, INotificationService notificationService, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var emailClaim = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name ?? "user@beautyplatform.com";
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            // Find specialist or staff member details to store
            Guid resolvedSpecialistId = request.SpecialistId;
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
                    if (staff.SpecialistId.HasValue)
                    {
                        resolvedSpecialistId = staff.SpecialistId.Value;
                        var linkedSpec = await context.Specialists.FirstOrDefaultAsync(s => s.Id == resolvedSpecialistId, ct);
                        specialistName = linkedSpec?.FullName ?? staff.FullName;
                    }
                    else
                    {
                        specialistName = staff.FullName;
                    }
                }
                else
                {
                    return Results.NotFound(new { message = "Specialist or Staff Member not found" });
                }
            }

            var booking = new Booking(
                resolvedSpecialistId,
                specialistName,
                request.ServiceName,
                request.Price,
                request.DurationMinutes,
                request.BookingDate,
                request.TimeSlot,
                userId,
                emailClaim,
                request.SalonId,
                request.SalonName
            );

            context.Bookings.Add(booking);
            await context.SaveChangesAsync(ct);

            try
            {
                await notificationService.SendNotificationToUserAsync(
                    resolvedSpecialistId,
                    "New Booking Created",
                    $"New appointment for {request.ServiceName} on {request.BookingDate:yyyy-MM-dd} at {request.TimeSlot}.",
                    ct);
            }
            catch
            {
                // Push failure shouldn't prevent HTTP 201 response
            }

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
            var activeStaffIds = await context.StaffMembers
                                              .Where(sm => specialistIds.Contains(sm.Id))
                                              .Select(sm => sm.Id)
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
                b.SalonId,
                b.SalonName,
                IsSpecialistDeleted = !activeSpecialistIds.Contains(b.SpecialistId) && !activeStaffIds.Contains(b.SpecialistId)
            });

            return Results.Ok(result);
        })
        .WithSummary("Get user bookings");

        group.MapGet("/specialist/{specialistId:guid}", async (Guid specialistId, [FromQuery] Guid? salonId, ApplicationDbContext context, CancellationToken ct) =>
        {
            // Build the set of related IDs to fetch bookings for this physical specialist
            var relatedIds = new List<Guid> { specialistId };
            Guid actualSpecialistId = specialistId;
            
            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == specialistId, ct);
            if (specialist != null)
            {
                var linkedStaffIds = await context.StaffMembers
                                                   .Where(sm => sm.SpecialistId == specialistId)
                                                   .Select(sm => sm.Id)
                                                   .ToListAsync(ct);
                relatedIds.AddRange(linkedStaffIds);
            }
            else
            {
                var staffMember = await context.StaffMembers.FirstOrDefaultAsync(sm => sm.Id == specialistId, ct);
                if (staffMember != null)
                {
                    if (staffMember.SpecialistId.HasValue)
                    {
                        actualSpecialistId = staffMember.SpecialistId.Value;
                        relatedIds.Add(actualSpecialistId);
                        
                        var otherLinkedStaffIds = await context.StaffMembers
                                                               .Where(sm => sm.SpecialistId == actualSpecialistId && sm.Id != staffMember.Id)
                                                               .Select(sm => sm.Id)
                                                               .ToListAsync(ct);
                        relatedIds.AddRange(otherLinkedStaffIds);
                    }
                }
            }

            // Get actual bookings
            var bookings = await context.Bookings
                                        .Where(b => relatedIds.Contains(b.SpecialistId))
                                        .ToListAsync(ct);

            // Get all staff member profiles linked to this specialist to find their salon shifts
            var staffShifts = await context.StaffMembers
                                           .Where(sm => sm.SpecialistId == actualSpecialistId && sm.WorkingHours != null)
                                           .ToListAsync(ct);

            var blockedBookings = new List<Booking>();
            foreach (var staff in staffShifts)
            {
                // If we are currently checking slots inside the salon where they work, do NOT block these hours!
                if (salonId.HasValue && salonId.Value == staff.SalonId)
                {
                    continue;
                }

                var salon = await context.Salons.FirstOrDefaultAsync(s => s.Id == staff.SalonId, ct);
                var salonName = salon?.SalonName ?? "Salon";
                var parsedShifts = WorkingHoursParser.Parse(staff.WorkingHours!);
                
                var today = DateTime.Today;
                for (int offset = 0; offset < 90; offset++)
                {
                    var date = today.AddDays(offset);
                    var dayOfWeek = date.DayOfWeek;

                    foreach (var shift in parsedShifts)
                    {
                        if (shift.Day == dayOfWeek)
                        {
                            var timeSlotStr = $"{shift.Start:hh\\:mm}-{shift.End:hh\\:mm}";
                            var virtualBooking = new Booking(
                                actualSpecialistId,
                                "BLOCKED_SLOT",
                                $"At {salonName}",
                                0,
                                (int)(shift.End - shift.Start).TotalMinutes,
                                date,
                                timeSlotStr,
                                Guid.Empty,
                                "system@beautyplatform.com",
                                staff.SalonId,
                                salonName
                            );
                            blockedBookings.Add(virtualBooking);
                        }
                    }
                }
            }

            // Combine actual and virtual/blocked bookings
            var allBookings = bookings.Concat(blockedBookings).ToList();

            // Look up user names for each actual booking
            var userIds = allBookings.Select(b => b.UserId).Distinct().ToList();
            var users = await context.Users
                                     .Where(u => userIds.Contains(u.Id))
                                     .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

            var result = allBookings.Select(b => new
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
        .WithSummary("Get specialist bookings");

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ApplicationDbContext context, INotificationService notificationService, CancellationToken ct) =>
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

            try
            {
                await notificationService.SendNotificationToUserAsync(
                    booking.SpecialistId,
                    "Booking Cancelled",
                    $"Appointment for {booking.ServiceName} on {booking.BookingDate:yyyy-MM-dd} at {booking.TimeSlot} has been cancelled by the client.",
                    ct);
            }
            catch
            {
                // Push failure shouldn't prevent successful cancel response
            }

            return Results.Ok(new { message = "Booking cancelled successfully" });
        })
        .WithSummary("Cancel a booking");

        group.MapPatch("/{id:guid}/no-show", async (Guid id, [FromQuery] bool isNoShow, ClaimsPrincipal principal, ApplicationDbContext context, INotificationService notificationService, CancellationToken ct) =>
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

            // Verify that the current user is authorized (either the specialist or the salon owner)
            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == userId, ct);
            bool isAuthorized = false;

            if (specialist != null)
            {
                isAuthorized = booking.SpecialistId == specialist.Id;
                if (!isAuthorized)
                {
                    // Check if the booking was made under a StaffMember ID linked to this specialist
                    var isLinkedStaff = await context.StaffMembers.AnyAsync(sm => sm.Id == booking.SpecialistId && sm.SpecialistId == specialist.Id, ct);
                    isAuthorized = isLinkedStaff;
                }
            }
            else
            {
                var salon = await context.Salons.Include(s => s.StaffMembers).FirstOrDefaultAsync(s => s.Id == userId, ct);
                if (salon != null)
                {
                    isAuthorized = booking.SalonId == salon.Id || 
                                   salon.StaffMembers.Any(sm => sm.Id == booking.SpecialistId) ||
                                   (booking.SpecialistId != Guid.Empty && salon.StaffMembers.Any(sm => sm.SpecialistId == booking.SpecialistId));
                }
            }

            if (!isAuthorized)
            {
                return Results.Forbid();
            }

            booking.MarkAsNoShow(isNoShow);
            await context.SaveChangesAsync(ct);

            try
            {
                string pushTitle = isNoShow ? "Booking Marked as No-Show" : "No-Show Cancelled";
                string pushMsg = isNoShow 
                    ? $"Your appointment for {booking.ServiceName} on {booking.BookingDate:yyyy-MM-dd} at {booking.TimeSlot} was marked as a no-show."
                    : $"Your appointment for {booking.ServiceName} on {booking.BookingDate:yyyy-MM-dd} at {booking.TimeSlot} no-show status has been removed.";

                await notificationService.SendNotificationToUserAsync(booking.UserId, pushTitle, pushMsg, ct);
            }
            catch
            {
                // Push failure shouldn't prevent successful no-show toggle response
            }

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
    string TimeSlot,
    Guid? SalonId = null,
    string? SalonName = null
);

public static class WorkingHoursParser
{
    public static List<(DayOfWeek Day, TimeSpan Start, TimeSpan End)> Parse(string workingHours)
    {
        var result = new List<(DayOfWeek, TimeSpan, TimeSpan)>();
        if (string.IsNullOrWhiteSpace(workingHours))
            return result;

        try
        {
            // Normalize spaces around the hyphen and remove extra spaces
            var normalized = Regex.Replace(workingHours, @"\s*-\s*", "-");
            normalized = Regex.Replace(normalized, @"\s*to\s*", "-");
            
            var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                if (TryParseTimeRange(normalized, out var start, out var end))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        result.Add(((DayOfWeek)i, start, end));
                    }
                }
                return result;
            }

            var daysPart = parts[0];
            var timePart = parts[1];

            if (!TryParseTimeRange(timePart, out var startTime, out var endTime))
                return result;

            var days = ParseDays(daysPart);
            foreach (var day in days)
            {
                result.Add((day, startTime, endTime));
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return result;
    }

    private static bool TryParseTimeRange(string range, out TimeSpan start, out TimeSpan end)
    {
        start = TimeSpan.Zero;
        end = TimeSpan.Zero;

        var parts = range.Split(new[] { '-', 't', 'o' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        if (TimeSpan.TryParse(parts[0].Trim(), out start) && TimeSpan.TryParse(parts[1].Trim(), out end))
        {
            return true;
        }
        return false;
    }

    private static List<DayOfWeek> ParseDays(string daysPart)
    {
        var result = new List<DayOfWeek>();
        var parts = daysPart.Split(new[] { ',', '&' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim().ToLowerInvariant();
            if (trimmed.Contains('-'))
            {
                var rangeParts = trimmed.Split('-');
                if (rangeParts.Length == 2)
                {
                    var startDay = MapDay(rangeParts[0]);
                    var endDay = MapDay(rangeParts[1]);
                    if (startDay.HasValue && endDay.HasValue)
                    {
                        int current = (int)startDay.Value;
                        int target = (int)endDay.Value;
                        while (true)
                        {
                            result.Add((DayOfWeek)current);
                            if (current == target) break;
                            current = (current + 1) % 7;
                        }
                    }
                }
            }
            else
            {
                var day = MapDay(trimmed);
                if (day.HasValue)
                {
                    result.Add(day.Value);
                }
            }
        }
        return result;
    }

    private static DayOfWeek? MapDay(string dayStr)
    {
        dayStr = dayStr.Trim().ToLowerInvariant();
        if (dayStr.StartsWith("mon")) return DayOfWeek.Monday;
        if (dayStr.StartsWith("tue")) return DayOfWeek.Tuesday;
        if (dayStr.StartsWith("wed")) return DayOfWeek.Wednesday;
        if (dayStr.StartsWith("thu")) return DayOfWeek.Thursday;
        if (dayStr.StartsWith("fri")) return DayOfWeek.Friday;
        if (dayStr.StartsWith("sat")) return DayOfWeek.Saturday;
        if (dayStr.StartsWith("sun")) return DayOfWeek.Sunday;
        return null;
    }
}
