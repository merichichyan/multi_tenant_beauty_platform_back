using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using multi_tenant_beauty_platform_back.Application.DTOs.Auth;
using multi_tenant_beauty_platform_back.Application.Services;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth").WithTags("Auth");

        authGroup.MapPost("/register", async ([FromBody] RegisterRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(request, ct);
            return Results.Ok(result);
        })
        .WithSummary("Register a new user (Legacy)")
        .WithDescription("Registers a new user and returns user details along with onboarding status.");

        authGroup.MapPost("/register/user", async ([FromBody] UserRegisterRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RegisterUserAsync(request, ct);
            return Results.Ok(result);
        })
        .WithSummary("Register a client user")
        .WithDescription("Registers a client user with extended profile fields and returns onboarding status.");

        authGroup.MapPost("/register/specialist", async ([FromBody] SpecialistRegisterRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RegisterSpecialistAsync(request, ct);
            return Results.Ok(result);
        })
        .WithSummary("Register an independent specialist")
        .WithDescription("Registers an independent specialist with branding, location, working hours, and services.");

        authGroup.MapPost("/register/salon", async ([FromBody] SalonRegisterRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RegisterSalonAsync(request, ct);
            return Results.Ok(result);
        })
        .WithSummary("Register a salon business")
        .WithDescription("Registers a salon business with branding, location, operating hours, staff members, and services.");

        authGroup.MapPost("/login", async ([FromBody] LoginRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(request, ct);
            return Results.Ok(result);
        })
        .WithSummary("Log in an existing user")
        .WithDescription("Authenticates a user and returns a JWT token along with onboarding status.");

        authGroup.MapPost("/select-role", async ([FromBody] SelectRoleRequestDto request, IAuthService authService, CancellationToken ct) =>
        {
            await authService.SelectRoleAsync(request, ct);
            return Results.Ok(new { message = "Role updated successfully.", role = request.Role });
        })
        .WithSummary("Select a user role")
        .WithDescription("Updates the role of a user during/after registration.");

        var usersGroup = app.MapGroup("/api/users").WithTags("Users");
        usersGroup.MapPatch("/onboarding/complete", [Authorize] async (ClaimsPrincipal principal, [FromQuery] Guid? userId, IAuthService authService, CancellationToken ct) =>
        {
            Guid targetUserId;
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                targetUserId = userId.Value;
            }
            else
            {
                var nameIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(nameIdClaim) || !Guid.TryParse(nameIdClaim, out targetUserId))
                {
                    return Results.Unauthorized();
                }
            }

            await authService.CompleteOnboardingAsync(targetUserId, ct);
            return Results.Ok(new { message = "Onboarding completed successfully.", isOnboardingCompleted = true });
        })
        .WithSummary("Complete onboarding for user")
        .WithDescription("Marks the authenticated user's onboarding status as completed.");

        return app;
    }
}
