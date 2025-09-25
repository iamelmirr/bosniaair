using System.Net;
using System.Text.Json;

namespace SarajevoAir.Api.Middleware;

/*
===========================================================================================
                              GLOBAL EXCEPTION HANDLING MIDDLEWARE
===========================================================================================

PURPOSE & ASP.NET CORE PIPELINE INTEGRATION:
Centralized exception handling za entire HTTP request pipeline.
Catches unhandled exceptions i converts them to proper HTTP responses.

MIDDLEWARE PIPELINE POSITION:
Should be registered FIRST u pipeline to catch all downstream exceptions:

┌─────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│   HTTP REQUEST      │────│  EXCEPTION MIDDLEWARE    │────│   DOWNSTREAM        │
│  (Client Browser)   │    │   (This Component)       │    │   MIDDLEWARE        │
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘
         │                              │                              │
         │                              ▼                              │
         │                   ┌─────────────────────┐                  │
         │                   │   CONTROLLERS       │ ◄────────────────┘
         │                   │   SERVICES          │
         │                   │   (Business Logic)  │
         │                   └─────────────────────┘
         │                              │
         │                              ▼ (Exception thrown)
         │                   ┌─────────────────────┐
         │  ◄────────────────│ STRUCTURED ERROR    │
         │                   │ RESPONSE (JSON)     │
         │                   └─────────────────────┘

CROSS-CUTTING CONCERNS:
- Consistent error response formatting
- Security: Prevents sensitive info leakage
- Observability: Structured logging za monitoring
- Client compatibility: RFC 7807 Problem Details format
- Development support: Detailed errors u dev environment

EXCEPTION MAPPING STRATEGY:
Maps .NET exception types to appropriate HTTP status codes
Provides consistent client experience regardless od internal error types
*/

/// <summary>
/// Global exception handler middleware za consistent error responses
/// Intercepts unhandled exceptions i converts to structured HTTP responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    /*
    === MIDDLEWARE PIPELINE DEPENDENCIES ===
    
    ASP.NET CORE MIDDLEWARE PATTERN:
    RequestDelegate _next - Points to next middleware u pipeline
    ILogger - Structured logging za exception tracking i diagnostics
    
    MIDDLEWARE LIFECYCLE:
    Constructor called once during app startup
    InvokeAsync called za every HTTP request
    */
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Constructor za middleware pipeline integration
    /// Called once during application startup
    /// </summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /*
    === MIDDLEWARE EXECUTION ENTRY POINT ===
    
    REQUEST PIPELINE FLOW:
    1. Middleware receives HTTP request
    2. Calls next middleware u chain
    3. If exception occurs, catches i handles gracefully
    4. Returns structured error response to client
    
    TRY-CATCH WRAPPER PATTERN:
    Wraps entire downstream pipeline execution
    Ensures no unhandled exceptions reach ASP.NET Core host
    Provides consistent error handling regardless od error source
    */
    
    /// <summary>
    /// Middleware execution method called za every HTTP request
    /// Wraps downstream pipeline sa exception handling
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            /*
            === DOWNSTREAM PIPELINE EXECUTION ===
            
            NORMAL FLOW:
            Calls next middleware u pipeline chain
            Could be: Authentication → Authorization → Controllers → etc.
            If no exceptions, request completes normally
            */
            await _next(context);
        }
        catch (Exception ex)
        {
            /*
            === EXCEPTION INTERCEPTION ===
            
            UNHANDLED EXCEPTION CAPTURE:
            Catches ALL unhandled exceptions od downstream middleware
            Logs exception details za debugging i monitoring
            Converts exception to structured HTTP response
            
            LOGGING STRATEGY:
            LogError ensures exception appears u monitoring systems
            Includes full exception details including stack trace
            Request context preserved za correlation
            */
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    /*
    === EXCEPTION TO HTTP RESPONSE CONVERSION ===
    
    STRUCTURED ERROR RESPONSE GENERATION:
    Converts .NET exceptions to HTTP-compliant error responses
    Uses RFC 7807 Problem Details standard za consistent format
    Provides appropriate HTTP status codes za different exception types
    */
    
    /// <summary>
    /// Converts caught exception to structured HTTP error response
    /// Implements RFC 7807 Problem Details specification
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        /*
        === RFC 7807 PROBLEM DETAILS FORMAT ===
        
        STANDARD COMPLIANCE:
        application/problem+json content type signals structured errors
        Enables clients to parse errors consistently
        Compatible sa modern HTTP client libraries
        */
        context.Response.ContentType = "application/problem+json";

        /*
        === EXCEPTION TYPE MAPPING ===
        
        HTTP STATUS CODE STRATEGY:
        Maps specific .NET exception types to appropriate HTTP codes
        Pattern matching enables precise error categorization
        Default to 500 Internal Server Error za unknown exceptions
        
        EXCEPTION CATEGORIZATION:
        - ArgumentException: Client sent invalid data (400 Bad Request)
        - KeyNotFoundException: Requested resource missing (404 Not Found)
        - UnauthorizedAccessException: Authentication required (401 Unauthorized)
        - NotImplementedException: Feature not available (501 Not Implemented)
        - TimeoutException: Request took too long (408 Request Timeout)
        - Default: System error (500 Internal Server Error)
        */
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

        /*
        === PROBLEM DETAILS STRUCTURE ===
        
        RFC 7807 COMPLIANT RESPONSE:
        - type: URI identifying error type (using about:blank za generic)
        - title: Human-readable error summary
        - status: HTTP status code za programmatic handling
        - detail: Specific error description (environment-dependent)
        - traceId: Request correlation identifier za debugging
        - timestamp: Error occurrence time za temporal analysis
        
        SECURITY CONSIDERATIONS:
        Production environment hides sensitive exception details
        Development environment shows full exception messages
        Prevents information leakage u production
        */
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

        /*
        === JSON SERIALIZATION ===
        
        RESPONSE FORMAT CONSISTENCY:
        CamelCase naming matches JavaScript conventions
        Ensures client-side compatibility
        Standard JSON serialization za predictable parsing
        */
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}