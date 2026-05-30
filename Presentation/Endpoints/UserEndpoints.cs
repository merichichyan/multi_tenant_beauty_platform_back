using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.Services;
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
            var specialist = await context.Specialists.FirstOrDefaultAsync(s => s.Id == userId, ct);
            if (specialist != null)
            {
                logoUrl = specialist.LogoUrl;
            }
            else
            {
                var salon = await context.Salons.FirstOrDefaultAsync(s => s.Id == userId, ct);
                if (salon != null)
                {
                    logoUrl = salon.LogoUrl;
                }
            }

            return Results.Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role,
                status = user.Status,
                logoUrl = logoUrl
            });
        })
        .RequireAuthorization()
        .WithSummary("Get current user profile info");

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

        group.MapPatch("/{id:guid}/status", async (Guid id, [FromQuery] string status, IUserService userService, CancellationToken ct) =>
        {
            try
            {
                await userService.UpdateUserStatusAsync(id, status, ct);
                return Results.Ok(new { message = "Status updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithSummary("Update user status")
        .WithDescription("Updates the status of a user (e.g. Verified, Rejected, Blocked).");

        return app;
    }
}
