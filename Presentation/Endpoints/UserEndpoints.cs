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

        group.MapGet("/me", async (ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
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

            string? logoUrl = null;
            string? address = null;
            string? description = null;
            string? socialMedias = null;
            string? preferredColors = null;
            string? workingHours = null;
            string? salonName = null;

            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == userId, ct);
            if (specialist != null)
            {
                logoUrl = specialist.LogoUrl;
                address = specialist.Address;
                description = specialist.Description;
                socialMedias = specialist.SocialMedias;
                preferredColors = specialist.PreferredColors;
                workingHours = specialist.WorkingHours;
            }
            else
            {
                var salon = await context.Salons.FirstOrDefaultAsync(s => s.Id == userId, ct);
                if (salon != null)
                {
                    logoUrl = salon.LogoUrl;
                    address = salon.Address;
                    description = salon.Description;
                    socialMedias = salon.SocialMedias;
                    preferredColors = salon.PreferredColors;
                    workingHours = salon.OperatingHours;
                    salonName = salon.SalonName;
                }
            }

            return Results.Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role,
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
                salonName = salonName,
                rating = specialist != null ? specialist.Rating : (double?)null
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
                specialist.UpdateSpecialistProfile(
                    request.Address ?? string.Empty,
                    request.Description,
                    request.SocialMedias,
                    request.LogoUrl,
                    request.PreferredColors,
                    request.WorkingHours
                );
            }
            else
            {
                var salon = await context.Salons.FirstOrDefaultAsync(s => s.Id == userId, ct);
                if (salon != null)
                {
                    salon.UpdateSalonProfile(
                        request.SalonName ?? request.FullName,
                        request.Address ?? string.Empty,
                        request.Description,
                        request.SocialMedias,
                        request.LogoUrl,
                        request.PreferredColors,
                        request.WorkingHours
                    );
                }
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
    string? Description,
    string? SocialMedias,
    string? LogoUrl,
    string? PreferredColors,
    string? WorkingHours,
    string? SalonName
);

public record UpdatePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UpdateStatusRequest(
    string Status,
    string? Reason
);
