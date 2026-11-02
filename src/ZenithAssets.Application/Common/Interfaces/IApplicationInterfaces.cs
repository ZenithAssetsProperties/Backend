using Microsoft.EntityFrameworkCore;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Property> Properties { get; }
    DbSet<PropertyMedia> PropertyMedia { get; }
    DbSet<PropertyDocument> PropertyDocuments { get; }
    DbSet<DocumentAccessRequest> DocumentAccessRequests { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<PaymentPlan> PaymentPlans { get; }
    DbSet<Inspection> Inspections { get; }
    DbSet<Lead> Leads { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTime
{
    DateTime UtcNow { get; }
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> AcquireLockAsync(string resourceKey, TimeSpan expiry, TimeSpan waitTime, CancellationToken cancellationToken = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public record InitializePaymentResult(bool Success, string Reference, string CheckoutUrl, string? ErrorMessage = null);

public interface IPaymentGatewayService
{
    Task<InitializePaymentResult> InitializePaymentAsync(
        string email,
        decimal amount,
        string currency,
        string reference,
        string callbackUrl,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    bool VerifyWebhookSignature(string payload, string signature);
}

public interface IStorageService
{
    Task<string> GeneratePreSignedDownloadUrlAsync(string storageKey, TimeSpan validDuration, string? downloadFileName = null);
    Task<string> UploadFileAsync(Stream fileStream, string storageKey, string contentType, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default);
    Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);
    Task SendWhatsAppMessageAsync(string toPhoneNumber, string messageText, CancellationToken cancellationToken = default);
}

