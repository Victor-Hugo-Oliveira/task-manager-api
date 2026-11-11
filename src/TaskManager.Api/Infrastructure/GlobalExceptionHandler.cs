using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Api.Infrastructure;

/// <summary>
/// Converts exceptions into RFC 9457 ProblemDetails responses so every error has the same shape.
/// </summary>
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException ex => new ValidationProblemDetails(
                ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            },

            NotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = ex.Message
            },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
                // No exception details here: never leak internals to clients.
            }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
