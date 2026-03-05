using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using KamSquare.KamScore.Application.Exceptions;
using KamSquare.KamScore.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace KamSquare.KamScore.Api.Middleware;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            NotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = ex.Message
            },
            ForbiddenException ex => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = ex.Message
            },
            UnauthorizedException ex => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = ex.Message
            },
            ValidationException ex => CreateValidationProblemDetails(ex),
            ArgumentException ex => CreateArgumentProblemDetails(ex),
            PhaseStateException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Phase State Conflict",
                Detail = ex.Message
            },
            ReferentialIntegrityException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Referential Integrity Conflict",
                Detail = ex.Message
            },
            CosmosException { StatusCode: System.Net.HttpStatusCode.PreconditionFailed } => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "The resource was modified by another request. Please reload and try again."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred."
            }
        };

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            if (exception is ForbiddenException)
            {
                var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                _logger.LogWarning("Authorization denied for user {UserId} on {Method} {Path}",
                    userId, context.Request.Method, context.Request.Path);
            }
            else if (exception is UnauthorizedException)
            {
                _logger.LogWarning("Authentication required for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            _logger.LogInformation("Request failed with {StatusCode}: {Detail}", problemDetails.Status, problemDetails.Detail);
        }

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }

    private ProblemDetails CreateArgumentProblemDetails(ArgumentException ex)
    {
        _logger.LogWarning(ex, "Argument exception: {Message}", ex.Message);
        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "The request contains invalid arguments."
        };
    }

    private static ProblemDetails CreateValidationProblemDetails(ValidationException ex)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))
        };

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        problemDetails.Extensions["errors"] = errors;

        return problemDetails;
    }
}
