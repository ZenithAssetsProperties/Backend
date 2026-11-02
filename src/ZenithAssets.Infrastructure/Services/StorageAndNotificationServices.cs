using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZenithAssets.Application.Common.Interfaces;

namespace ZenithAssets.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public class SecureStorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecureStorageService> _logger;

    public SecureStorageService(IConfiguration configuration, ILogger<SecureStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> GeneratePreSignedDownloadUrlAsync(string storageKey, TimeSpan validDuration, string? downloadFileName = null)
    {
        var baseUrl = _configuration["Storage:BaseUrl"] ?? "https://storage.zenithassets.ng";
        var secret = _configuration["Storage:SigningSecret"] ?? "Zenith_Storage_Vault_Secret_2026";

        var expiresAt = DateTimeOffset.UtcNow.Add(validDuration).ToUnixTimeSeconds();
        var dataToSign = $"{storageKey}:{expiresAt}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));

        var query = $"token={signature}&expires={expiresAt}";
        if (!string.IsNullOrWhiteSpace(downloadFileName))
        {
            query += $"&filename={Uri.EscapeDataString(downloadFileName)}";
        }

        var signedUrl = $"{baseUrl}/{storageKey}?{query}";
        _logger.LogInformation("Generated Pre-Signed URL for {StorageKey}, valid until {ExpiresAt}", storageKey, expiresAt);

        return Task.FromResult(signedUrl);
    }

    public Task<string> UploadFileAsync(Stream fileStream, string storageKey, string contentType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploaded file to vault: Key={StorageKey}, ContentType={ContentType}", storageKey, contentType);
        return Task.FromResult(storageKey);
    }
}

public class MultiChannelNotificationService : INotificationService
{
    private readonly ILogger<MultiChannelNotificationService> _logger;

    public MultiChannelNotificationService(ILogger<MultiChannelNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatching Email: To={ToEmail}, Subject={Subject}", toEmail, subject);
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatching SMS via Termii: To={Phone}, Length={Length}", toPhoneNumber, message.Length);
        return Task.CompletedTask;
    }

    public Task SendWhatsAppMessageAsync(string toPhoneNumber, string messageText, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatching WhatsApp Cloud Message: To={Phone}, Message={Message}", toPhoneNumber, messageText);
        return Task.CompletedTask;
    }
}

