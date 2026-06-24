using System.Net;
using FluentValidation;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled request exception");
            await WriteErrorAsync(context, ex);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (status, message, errors) = exception switch
        {
            ValidationException validation => (HttpStatusCode.BadRequest, "Validation failed", validation.Errors.Select(x => x.ErrorMessage).ToArray()),
            ValidationAppException validation => (HttpStatusCode.BadRequest, validation.Message, validation.Errors.SelectMany(x => x.Value).ToArray()),
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message, Array.Empty<string>()),
            UnauthorizedAppException unauthorized => (HttpStatusCode.Unauthorized, unauthorized.Message, Array.Empty<string>()),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, forbidden.Message, Array.Empty<string>()),
            BusinessRuleException business => (HttpStatusCode.BadRequest, business.Message, Array.Empty<string>()),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", Array.Empty<string>())
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, errors));
    }
}
