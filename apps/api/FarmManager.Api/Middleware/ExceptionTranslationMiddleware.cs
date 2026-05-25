using FarmManager.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FarmManager.Api.Middleware;

/// <summary>
/// Translates known domain + validation exceptions into RFC-7807 ProblemDetails responses.
/// Phase C.2 surfaces 409 conflicts in a shape the offline sync queue understands.
/// </summary>
public sealed class ExceptionTranslationMiddleware(RequestDelegate next, ILogger<ExceptionTranslationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ConflictException ex)
        {
            await Write(context, StatusCodes.Status409Conflict, new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
                Type = $"urn:farm-manager:conflict:{ex.Code}",
            });
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await Write(context, StatusCodes.Status400BadRequest, new ValidationProblemDetails(errors)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
            });
        }
        catch (InvalidOperationException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, new ProblemDetails
            {
                Title = "Invalid operation",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await Write(context, StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
            });
        }
    }

    private static Task Write(HttpContext context, int status, ProblemDetails details)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(details);
    }
}
