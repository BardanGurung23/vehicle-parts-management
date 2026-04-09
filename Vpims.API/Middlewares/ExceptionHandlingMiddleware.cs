using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.Common.Exceptions;

namespace Vpims.API.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Request failed: {Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        int statusCode = exception switch
        {
            AppValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Title = exception switch
            {
                AppValidationException => "Validation failed",
                NotFoundException => "Resource not found",
                UnauthorizedAccessException => "Unauthorized",
                _ => "Unexpected server error"
            },
            Detail = exception.Message,
            Status = statusCode
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}