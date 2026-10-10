using ex1.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ex1.Middleware;

/// <summary>
/// Turns typed exceptions into RFC 7807 problem responses, so controllers don't need
/// try/catch blocks or to match on exception message text.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                BusinessRuleException => (StatusCodes.Status400BadRequest, "Business rule violated"),
                _ => (StatusCodes.Status500InternalServerError, "Internal server error"),
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("{Title} on {Method} {Path}: {Message}", title, context.Request.Method, context.Request.Path, ex.Message);

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Never leak internal error details to the client
                Detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message,
                Instance = context.Request.Path,
            };

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
    }
}
