using multi_tenant_beauty_platform_back.Application.Services;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class SpecialistEndpoints
{
    public static IEndpointRouteBuilder MapSpecialistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/specialists")
                       .WithTags("Specialists")
                       .RequireAuthorization();

        group.MapGet("/", async (int page, ISpecialistService service, CancellationToken ct) =>
        {
            if (page < 1) page = 1;
            var result = await service.GetPagedAsync(page, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get paginated list of specialists (10 per page)")
        .WithDescription("Returns specialists with their services. Pass ?page=1, ?page=2, etc.");

        group.MapGet("/{id:guid}", async (Guid id, ISpecialistService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("Get a specialist by ID");

        return app;
    }
}
