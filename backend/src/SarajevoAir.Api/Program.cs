using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SarajevoAir.Api.Middleware;
using SarajevoAir.Api.Configuration;
using SarajevoAir.Api.Services;
using SarajevoAir.Application.Interfaces;
using SarajevoAir.Application.Services;
using SarajevoAir.Domain.Aqi;
using SarajevoAir.Infrastructure.Data;
using SarajevoAir.Infrastructure.OpenAq;
using SarajevoAir.Worker;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// CORS configuration
var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Controllers and API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? builder.Configuration["CONNECTION_STRING"] 
                       ?? "Host=localhost;Database=sarajevoair;Username=dev;Password=dev";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
    
// Register IAppDbContext
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Caching
builder.Services.AddMemoryCache();

// Configuration
builder.Services.Configure<AqicnConfiguration>(builder.Configuration.GetSection("Aqicn"));

// HTTP clients with Polly
builder.Services.AddHttpClient<IAqicnClient, AqicnClient>()
    .AddStandardResilienceHandler();

// Add temporary stub for IOpenAqClient to maintain compatibility
builder.Services.AddHttpClient<IOpenAqClient, OpenAqClient>()
    .AddStandardResilienceHandler();

// Application services - all removed after simplification
// IMeasurementService removed - no longer needed after simplification
// IAqiCalculator removed - no longer used 
// IShareService removed - using Web Share API instead

// Background services disabled - frontend will trigger data saving
// builder.Services.AddHostedService<AqiBackgroundService>();

// Health checks  
builder.Services.AddHealthChecks();

// Rate limiting will be configured later

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

// Exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS
app.UseCors("FrontendOnly");

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SarajevoAir API V1");
        c.RoutePrefix = "swagger";
    });
}

// Static files (for admin dashboard)
app.UseStaticFiles();

app.UseRouting();

// API routes
app.MapControllers();

// Health checks
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
                duration = x.Value.Duration.ToString()
            }),
            duration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Database migration on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration failed");
    }
}

Log.Information("SarajevoAir API starting up...");
app.Run();

public partial class Program { } // For integration testing