using Microsoft.AspNetCore.Mvc;
using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Application.Services;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/onboarding")
                       .WithTags("Onboarding");

        group.MapPost("/", async ([FromBody] OnboardingRequestDto request, IOnboardingService onboardingService, CancellationToken ct) =>
        {
            var result = await onboardingService.SubmitOnboardingAsync(request, ct);
            return Results.Created($"/api/v1/onboarding/{result.Id}", result);
        })
        .WithSummary("Submit onboarding preferences")
        .WithDescription("Registers or updates onboarding preferences (Language, Role, Timezone) for a device/user.");

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

        return app;
    }
}
