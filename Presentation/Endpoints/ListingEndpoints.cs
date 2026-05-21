using multi_tenant_beauty_platform_back.Application.Services;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

// NOTE: This file previously registered GET /api/specialists and GET /api/salons via IListingService.
// Those routes now live in SpecialistEndpoints and SalonEndpoints with full pagination support.
// This class is intentionally left empty to avoid duplicate route conflicts.
public static class ListingEndpoints
{
    public static IEndpointRouteBuilder MapListingEndpoints(this IEndpointRouteBuilder app)
    {
        // Routes migrated to:
        //   GET /api/specialists?page=N  (SpecialistEndpoints)
        //   GET /api/salons?page=N       (SalonEndpoints)
        return app;
    }
}
