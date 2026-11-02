using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using ZenithAssets.Application;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Infrastructure;
using ZenithAssets.WebApi.Middleware;
using ZenithAssets.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Register Application & Infrastructure Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 3. API Controllers & Routing
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();

// 4. Swagger / OpenAPI Configuration with JWT Security Scheme
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Zenith Assets Properties API",
        Version = "v1",
        Description = "Enterprise Real Estate Brokerage & Property Transaction Platform (Clean Architecture in .NET)",
        Contact = new OpenApiContact
        {
            Name = "Zenith Asset Group Technology Team",
            Email = "tech@zenithassets.ng"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Format: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. Multi-Portal CORS Policies
builder.Services.AddCors(options =>
{
    options.AddPolicy("ZenithPortalsPolicy", policy =>
    {
        policy.WithOrigins(
            "https://zenithassets.ng",
            "https://app.zenithassets.ng",
            "https://admin.zenithassets.ng",
            "https://agent.zenithassets.ng",
            "http://localhost:3000",
            "http://localhost:3001",
            "http://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// 6. Sliding-Window Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global sliding window limiter: 120 req/min per IP
    options.AddPolicy("GlobalSlidingWindow", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// 7. Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP Request Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zenith Assets API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("ZenithPortalsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Service = "Zenith Assets Properties Transaction API",
    Version = "1.0.0",
    Status = "Operational",
    Documentation = "/swagger",
    Environment = app.Environment.EnvironmentName,
    TimestampUtc = DateTime.UtcNow
}));

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
