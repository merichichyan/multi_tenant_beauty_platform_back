using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using multi_tenant_beauty_platform_back.Domain.Exceptions;

namespace multi_tenant_beauty_platform_back.Presentation.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unexpected error occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is DomainException domainException)
        {
            httpContext.Response.StatusCode = domainException.StatusCode;
            problemDetails.Title = domainException switch
            {
                NotFoundException => "Resource Not Found",
                ValidationException => "Validation Failure",
                _ => "Domain Rule Violation"
            };
            problemDetails.Status = domainException.StatusCode;
            problemDetails.Detail = domainException.Message;

            if (domainException is ValidationException validationException && validationException.Errors.Count > 0)
            {
                problemDetails.Extensions["errors"] = validationException.Errors;
            }
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Internal Server Error";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = "An unexpected error occurred while processing your request.";
        }

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
