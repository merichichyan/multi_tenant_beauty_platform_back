using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class LetterEndpoints
{
    public static IEndpointRouteBuilder MapLetterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/letters").WithTags("Letters");

        // POST /api/letters: Requires authorization
        group.MapPost("/", [Authorize] async ([FromBody] CreateLetterRequest request, ClaimsPrincipal principal, ApplicationDbContext context, CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var emailClaim = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name ?? "user@beautyplatform.com";
            var nameClaim = principal.FindFirstValue(ClaimTypes.Name) ?? "User";

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { message = "Message cannot be empty." });
            }

            var letter = new Letter(userId, emailClaim, nameClaim, request.Message);

            context.Letters.Add(letter);
            await context.SaveChangesAsync(ct);

            return Results.Created($"/api/letters/{letter.Id}", letter);
        })
        .WithSummary("Create a new support letter from user");

        // GET /api/letters: Open endpoint for admin app
        group.MapGet("/", async (ApplicationDbContext context, CancellationToken ct) =>
        {
            var letters = await context.Letters
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(letters);
        })
        .WithSummary("Retrieve all support letters");

        return app;
    }
}

public record CreateLetterRequest(string Message);
