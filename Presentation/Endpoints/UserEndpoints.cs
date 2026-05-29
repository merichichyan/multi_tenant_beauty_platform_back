using Microsoft.AspNetCore.Mvc;
using multi_tenant_beauty_platform_back.Application.Services;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

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
