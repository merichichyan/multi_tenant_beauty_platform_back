using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/onboarding")
                       .WithTags("Onboarding");

        group.MapPost("/", async ([FromBody] OnboardingRequestDto request, IOnboardingService onboardingService, CancellationToken ct) =>
        {
            var result = await onboardingService.SubmitOnboardingAsync(request, ct);
            return Results.Created($"/api/onboarding/{result.Id}", result);
        })
        .WithSummary("Submit onboarding preferences")
        .WithDescription("Registers or updates onboarding preferences (Language, Timezone, NotificationsAllowed) for a device/user.");

        group.MapGet("/{id:guid}", async (Guid id, IOnboardingService onboardingService, CancellationToken ct) =>
        {
            var result = await onboardingService.GetOnboardingByIdAsync(id, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get onboarding by ID")
        .WithDescription("Retrieves a specific onboarding submission by its unique identifier.");

        group.MapGet("/", async (IOnboardingService onboardingService, CancellationToken ct) =>
        {
            var result = await onboardingService.GetAllOnboardingsAsync(ct);
            return Results.Ok(result);
        })
        .WithSummary("Get all onboarding submissions")
        .WithDescription("Retrieves all onboarded users/devices in the system.");

        group.MapGet("/debug-db", async (ApplicationDbContext context, CancellationToken ct) =>
        {
            var users = await context.Users.Select(u => new { u.Id, u.Email, u.DeviceId, u.IsOnboardingCompleted }).ToListAsync(ct);
            var onboardings = await context.OnboardingSubmissions.Select(o => new { o.Id, o.DeviceId, o.Language }).ToListAsync(ct);
            return Results.Ok(new { users, onboardings });
        });

        return app;
    }
}
