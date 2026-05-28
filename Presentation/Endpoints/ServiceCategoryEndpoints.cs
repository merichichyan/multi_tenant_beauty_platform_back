using Microsoft.AspNetCore.Mvc;
using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Application.Services;

namespace multi_tenant_beauty_platform_back.Presentation.Endpoints;

public static class ServiceCategoryEndpoints
{
    public static IEndpointRouteBuilder MapServiceCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-categories")
                       .WithTags("ServiceCategories");

        group.MapGet("/", async (HttpContext httpContext, IServiceCategoryService service, CancellationToken ct) =>
        {
            var lang = GetLanguage(httpContext);
            var result = await service.GetAllAsync(lang, ct);
            return Results.Ok(result);
        })
        .WithSummary("Get all service categories");

        group.MapGet("/{id:guid}", async (Guid id, HttpContext httpContext, IServiceCategoryService service, CancellationToken ct) =>
        {
            var lang = GetLanguage(httpContext);
            var result = await service.GetByIdAsync(id, lang, ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("Get service category by ID");

        group.MapPost("/", async ([FromBody] ServiceCategoryRequestDto request, IServiceCategoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return Results.Created($"/api/service-categories/{result.Id}", result);
        })
        .WithSummary("Create a service category");

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] ServiceCategoryRequestDto request, IServiceCategoryService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("Update a service category");

        group.MapDelete("/{id:guid}", async (Guid id, IServiceCategoryService service, CancellationToken ct) =>
        {
            var success = await service.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Delete a service category");

        return app;
    }

    private static string GetLanguage(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("lang", out var langVal) && !string.IsNullOrEmpty(langVal))
        {
            var lang = langVal.ToString().ToLower();
            if (lang == "hy" || lang == "ru" || lang == "en")
            {
                return lang;
            }
        }

        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            if (acceptLanguage.Contains("hy", StringComparison.OrdinalIgnoreCase)) return "hy";
            if (acceptLanguage.Contains("ru", StringComparison.OrdinalIgnoreCase)) return "ru";
            if (acceptLanguage.Contains("en", StringComparison.OrdinalIgnoreCase)) return "en";
        }

        return "en";
    }
}
