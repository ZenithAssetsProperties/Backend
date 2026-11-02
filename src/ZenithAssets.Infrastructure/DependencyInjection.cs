using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Infrastructure.Caching;
using ZenithAssets.Infrastructure.Identity;
using ZenithAssets.Infrastructure.Payments;
using ZenithAssets.Infrastructure.Persistence;
using ZenithAssets.Infrastructure.Services;

namespace ZenithAssets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. PostgreSQL Database with Entity Framework Core
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=zenith_assets_db;Username=zenith_admin;Password=zenith_secure_password_2026";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // 2. Redis Connection (Optional / Graceful fallback if offline)
        var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379,password=zenith_redis_secret,abortConnect=false";
        try
        {
            var multiplexer = ConnectionMultiplexer.Connect(redisConn);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        }
        catch
        {
            // Graceful fallback when redis is not available in local test mode
            services.AddSingleton<IConnectionMultiplexer>(provider => null!);
        }

        // 3. Core Services
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IDistributedLockService, DistributedLockService>();
        services.AddScoped<IPaymentGatewayService, PaystackGatewayService>();
        services.AddScoped<IStorageService, SecureStorageService>();
        services.AddScoped<INotificationService, MultiChannelNotificationService>();

        // 4. JWT Authentication
        var jwtSecret = configuration["Jwt:SecretKey"] ?? "Zenith_Assets_Super_Secret_Key_2026_Enterprise_Edition_Must_Be_Long";
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "ZenithAssets.Api",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "ZenithAssets.Clients",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }
}
