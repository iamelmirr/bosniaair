using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SarajevoAir.Api.Configuration;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Middleware;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();
var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";

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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? builder.Configuration["CONNECTION_STRING"]
                       ?? "Data Source=sarajevoair-aqi.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.Configure<AqicnConfiguration>(builder.Configuration.GetSection("Aqicn"));

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
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();
builder.Services.AddScoped<IAirQualityService>(sp => sp.GetRequiredService<AirQualityService>());
builder.Services.AddHostedService<AirQualityScheduler>();
builder.Services.AddHealthChecks();
var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("FrontendOnly");
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
app.MapControllers();
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
app.Run();

public partial class Program { }