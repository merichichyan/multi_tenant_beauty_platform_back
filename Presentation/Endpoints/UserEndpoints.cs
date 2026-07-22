using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/me", async (ClaimsPrincipal principal, ApplicationDbContext context, Microsoft.AspNetCore.Http.HttpContext httpContext, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            var lang = "en";
            if (httpContext.Request.Query.TryGetValue("lang", out var langVal) && !string.IsNullOrEmpty(langVal))
            {
                var l = langVal.ToString().ToLower();
                if (l == "hy" || l == "ru" || l == "en") lang = l;
            }
            else
            {
                var acceptLanguage = httpContext.Request.Headers["Accept-Language"].ToString();
                if (!string.IsNullOrEmpty(acceptLanguage))
                {
                    if (acceptLanguage.Contains("hy", StringComparison.OrdinalIgnoreCase)) lang = "hy";
                    else if (acceptLanguage.Contains("ru", StringComparison.OrdinalIgnoreCase)) lang = "ru";
                    else if (acceptLanguage.Contains("en", StringComparison.OrdinalIgnoreCase)) lang = "en";
                }
            }

            string? logoUrl = null;
            string? address = null;
            string? description = null;
            string? socialMedias = null;
            string? preferredColors = null;
            string? workingHours = null;
            string? salonName = null;
            double? latitude = null;
            double? longitude = null;

            Guid? salonId = null;
            Salon? salon = null;

            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == userId, ct);
            if (specialist != null)
            {
                logoUrl = specialist.LogoUrl;
                address = LocalizationHelper.LocalizeString(specialist.Address, lang);
                description = LocalizationHelper.LocalizeString(specialist.Description, lang);
                socialMedias = specialist.SocialMedias;
                preferredColors = specialist.PreferredColors;
                workingHours = LocalizationHelper.LocalizeString(specialist.WorkingHours, lang);
                latitude = specialist.Latitude;
                longitude = specialist.Longitude;
                salonId = specialist.SalonId;

                if (specialist.SalonId.HasValue)
                {
                    var attachedSalon = await context.Salons.FirstOrDefaultAsync(s => s.Id == specialist.SalonId.Value, ct);
                    if (attachedSalon != null)
                    {
                        salonName = LocalizationHelper.LocalizeString(attachedSalon.SalonName, lang);
                    }
                }
            }
            else
            {
                salon = await context.Salons.FirstOrDefaultAsync(s => s.Id == userId, ct);
                if (salon != null)
                {
                    logoUrl = salon.LogoUrl;
                    address = LocalizationHelper.LocalizeString(salon.Address, lang);
                    description = LocalizationHelper.LocalizeString(salon.Description, lang);
                    socialMedias = salon.SocialMedias;
                    preferredColors = salon.PreferredColors;
                    workingHours = LocalizationHelper.LocalizeString(salon.OperatingHours, lang);
                    salonName = LocalizationHelper.LocalizeString(salon.SalonName, lang);
                    latitude = salon.Latitude;
                    longitude = salon.Longitude;
                    salonId = salon.Id;
                }
            }

            string? rejectionReason = null;
            if (user.Status == "Rejected")
            {
                var latestLetter = await context.Letters
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.CreatedAt)
                    .FirstOrDefaultAsync(ct);
                rejectionReason = latestLetter?.Message;
            }

            return Results.Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                role = specialist != null ? "specialist" : (salon != null ? "salon" : user.Role),
                status = user.Status,
                phone = user.Phone,
                gender = user.Gender,
                birthday = user.Birthday,
                logoUrl = logoUrl,
                address = address,
                description = description,
                socialMedias = socialMedias,
                preferredColors = preferredColors,
                workingHours = workingHours,
                salonId = salonId,
                salonName = salonName,
                latitude = latitude,
                longitude = longitude,
                rating = specialist != null ? specialist.Rating : (double?)null,
                rejectionReason = rejectionReason
            });
        })
        .RequireAuthorization()
        .WithSummary("Get current user profile info");

        group.MapDelete("/me", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Account deleted successfully" });
        })
        .RequireAuthorization()
        .WithSummary("Delete current user account");

        group.MapPut("/profile", async ([FromBody] UpdateProfileRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found." });
            }

            // Check if email already in use by another user
            var emailLower = request.Email.ToLowerInvariant().Trim();
            var emailInUse = await context.Users.AnyAsync(u => u.Email == emailLower && u.Id != userId, ct);
            if (emailInUse)
            {
                return Results.BadRequest(new { message = "Email is already registered by another account." });
            }

            // Base user updates
            user.UpdateProfile(request.Email, request.FullName, request.Phone, request.Gender, request.Birthday);

            // Specialist / Salon updates
            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == userId, ct);
            if (specialist != null)
            {
                var oldSalonId = specialist.SalonId;
                
                specialist.UpdateSpecialistProfile(
                    request.Address ?? string.Empty,
                    request.Latitude,
                    request.Longitude,
                    request.Description,
                    request.SocialMedias,
                    request.LogoUrl,
                    request.PreferredColors,
                    request.WorkingHours,
                    request.SalonId
                );
                
                // Sync StaffMember record
                if (oldSalonId != request.SalonId)
                {
                    // Remove from old salon
                    if (oldSalonId.HasValue)
                    {
                        var oldStaff = await context.StaffMembers.FirstOrDefaultAsync(sm => sm.SpecialistId == specialist.Id && sm.SalonId == oldSalonId.Value, ct);
                        if (oldStaff != null) context.StaffMembers.Remove(oldStaff);
                    }
                }
                
                // Always ensure they are in the new salon (if set), in case it was missing
                if (request.SalonId.HasValue)
                {
                    var exists = await context.StaffMembers.AnyAsync(sm => sm.SpecialistId == specialist.Id && sm.SalonId == request.SalonId.Value, ct);
                    if (!exists)
                    {
                        var newStaff = new multi_tenant_beauty_platform_back.Domain.Entities.StaffMember(
                            request.SalonId.Value, 
                            request.FullName, 
                            request.Description ?? "Specialist", 
                            request.LogoUrl, 
                            request.WorkingHours ?? "09:00-18:00", 
                            "Active", 
                            specialist.Id);
                        context.StaffMembers.Add(newStaff);
                    }
                }
            }
            else
            {
                var salon = await context.Salons.FirstOrDefaultAsync(s => s.Id == userId, ct);
                if (salon != null)
                {
                    salon.UpdateSalonProfile(
                        request.SalonName ?? request.FullName,
                        request.Address ?? string.Empty,
                        request.Latitude,
                        request.Longitude,
                        request.Description,
                        request.SocialMedias,
                        request.LogoUrl,
                        request.PreferredColors,
                        request.WorkingHours
                    );
                }
            }

            if (user.Status == "Rejected")
            {
                user.UpdateStatus("Pending");
            }

            await context.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Profile updated successfully.", user });
        })
        .RequireAuthorization()
        .WithSummary("Update logged-in user profile");

        group.MapPut("/password", async ([FromBody] UpdatePasswordRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found." });
            }

            // Verify current password matches
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Results.BadRequest(new { message = "Current password is incorrect." });
            }

            // Hash and update new password
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatePasswordHash(newPasswordHash);

            await context.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Password updated successfully." });
        })
        .RequireAuthorization()
        .WithSummary("Update logged-in user password");

        group.MapGet("/", async ([FromQuery] string? status, [FromQuery] int pageNumber, [FromQuery] int pageSize, IUserService userService, CancellationToken ct) =>
        {
            // Default pagination values if not provided
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var result = await userService.GetUsersAsync(status, pageNumber, pageSize, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get paginated list of users")
        .WithDescription("Retrieves a paginated list of users, optionally filtered by status.");

        group.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] UpdateStatusRequest request, IUserService userService, ApplicationDbContext context, CancellationToken ct) =>
        {
            try
            {
                await userService.UpdateUserStatusAsync(id, request.Status, ct);

                if (request.Status == "Rejected" && !string.IsNullOrWhiteSpace(request.Reason))
                {
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
                    if (user != null)
                    {
                        var letter = new Letter(user.Id, user.Email, user.FullName, request.Reason);
                        context.Letters.Add(letter);
                        await context.SaveChangesAsync(ct);
                    }
                }

                return Results.Ok(new { message = "Status updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithSummary("Update user status")
        .WithDescription("Updates the status of a user (e.g. Verified, Rejected, Blocked).");

        group.MapPut("/{id:guid}/salon", async (Guid id, [FromBody] UpdateUserSalonRequest request, ApplicationDbContext context, CancellationToken ct) =>
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null || user.Role != "specialist")
            {
                return Results.NotFound(new { message = "Specialist not found." });
            }

            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (specialist != null)
            {
                var oldSalonId = specialist.SalonId;
                
                // We update only the SalonId
                specialist.UpdateSpecialistProfile(
                    specialist.Address ?? string.Empty,
                    specialist.Latitude,
                    specialist.Longitude,
                    specialist.Description,
                    specialist.SocialMedias,
                    specialist.LogoUrl,
                    specialist.PreferredColors,
                    specialist.WorkingHours,
                    request.SalonId
                );
                
                // Sync StaffMember record
                if (oldSalonId != request.SalonId)
                {
                    // Remove from old salon
                    if (oldSalonId.HasValue)
                    {
                        var oldStaff = await context.StaffMembers.FirstOrDefaultAsync(sm => sm.SpecialistId == specialist.Id && sm.SalonId == oldSalonId.Value, ct);
                        if (oldStaff != null) context.StaffMembers.Remove(oldStaff);
                    }
                    
                    // Add to new salon
                    if (request.SalonId.HasValue)
                    {
                        var exists = await context.StaffMembers.AnyAsync(sm => sm.SpecialistId == specialist.Id && sm.SalonId == request.SalonId.Value, ct);
                        if (!exists)
                        {
                            var newStaff = new multi_tenant_beauty_platform_back.Domain.Entities.StaffMember(
                                request.SalonId.Value, 
                                user.FullName, 
                                specialist.Description ?? "Specialist", 
                                specialist.LogoUrl, 
                                specialist.WorkingHours ?? "09:00-18:00", 
                                "Active", 
                                specialist.Id);
                            context.StaffMembers.Add(newStaff);
                        }
                    }
                }
                
                await context.SaveChangesAsync(ct);
                return Results.Ok(new { message = "Salon updated successfully." });
            }

            return Results.NotFound(new { message = "Specialist profile not found." });
        })
        .WithSummary("Update a specialist's salon (Admin)");

        group.MapGet("/my-letters", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var letters = await context.Letters
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(letters);
        })
        .RequireAuthorization()
        .WithSummary("Get letters addressed to current user");

        return app;
    }
}

public record UpdateProfileRequest(
    string Email,
    string FullName,
    string? Phone,
    String? Gender,
    DateTime? Birthday,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? Description,
    string? SocialMedias,
    string? LogoUrl,
    string? PreferredColors,
    string? WorkingHours,
    string? SalonName,
    Guid? SalonId = null
);

public record UpdatePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UpdateStatusRequest(
    string Status,
    string? Reason
);

public record UpdateUserSalonRequest(
    Guid? SalonId
);
