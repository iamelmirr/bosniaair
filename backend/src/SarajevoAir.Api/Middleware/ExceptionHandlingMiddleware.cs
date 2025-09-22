using System.Net;
using System.Text.Json;

namespace SarajevoAir.Api.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title) = exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            NotImplementedException => (HttpStatusCode.NotImplemented, "Not Implemented"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Request Timeout"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            type = "about:blank",
            title,
            status = (int)statusCode,
            detail = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment() 
                ? exception.Message 
                : "An error occurred while processing your request.",
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}