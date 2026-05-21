using multi_tenant_beauty_platform_back.Application.Services;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class SalonEndpoints
{
    public static IEndpointRouteBuilder MapSalonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/salons")
                       .WithTags("Salons")
                       .RequireAuthorization();

        group.MapGet("/", async (int page, ISalonService service, CancellationToken ct) =>
        {
            if (page < 1) page = 1;
            var result = await service.GetPagedAsync(page, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get paginated list of salons (10 per page)")
        .WithDescription("Returns salons with their staff members and services. Pass ?page=1, ?page=2, etc.");

        group.MapGet("/{id:guid}", async (Guid id, ISalonService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("Get a salon by ID");

        return app;
    }
}
