using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SarajevoAir.Api.Configuration;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Middleware;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Services;
using Serilog;

/// <summary>
/// Main entry point for the SarajevoAir API. Sets up ASP.NET Core with services, middleware, and background workers.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// Early logging setup so we capture any startup issues
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();
var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";

// CORS policy allowing only specified frontend origins for security
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "http://localhost:5000", "http://localhost:5001", frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// MVC controllers with custom JSON serialization (camelCase, enums as strings, UTC dates)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();

// OpenAPI/Swagger setup for API documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SarajevoAir API",
        Version = "v1",
        Description = "Air quality monitoring API for Sarajevo and surrounding areas",
        Contact = new OpenApiContact
        {
            Name = "SarajevoAir Team",
            Url = new Uri("https://sarajevoair.vercel.app")
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? builder.Configuration["CONNECTION_STRING"]
                       ?? "Data Source=sarajevoair-aqi.db";

// Database setup with SQLite, falling back to embedded file if no connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Bind WAQI config section
builder.Services.Configure<AqicnConfiguration>(builder.Configuration.GetSection("Aqicn"));

// Typed HTTP client for WAQI API with resilience and custom headers
builder.Services.AddHttpClient<AirQualityService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration.GetValue<string>("Aqicn:ApiUrl") ?? "https://api.waqi.info/";
        client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + '/');
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "SarajevoAir/1.0");
    })
    .AddStandardResilienceHandler();

// Register data access and business logic services
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();
builder.Services.AddScoped<IAirQualityService>(sp => sp.GetRequiredService<AirQualityService>());
builder.Services.AddHostedService<AirQualityScheduler>();
builder.Services.AddHealthChecks();

// Build the app and configure middleware pipeline
var app = builder.Build();
app.UseSerilogRequestLogging(); // Log all requests
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Global error handling
app.UseCors("FrontendOnly"); // Apply CORS policy

// Development-only Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SarajevoAir API V1");
        c.RoutePrefix = "swagger";
    });
}
app.UseStaticFiles();
app.UseRouting();
app.MapControllers(); // Register API endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration
            }),
            duration = report.TotalDuration
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

// Ensure SQLite schema exists before handling requests
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database ready");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize database");
    }
}

Log.Information("SarajevoAir API starting up...");
app.Run(); // Start the web server and background services

public partial class Program { }