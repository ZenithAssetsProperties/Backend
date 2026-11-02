using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZenithAssets.Application.Common.Interfaces;

namespace ZenithAssets.Infrastructure.Payments;

public class PaystackGatewayService : IPaymentGatewayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaystackGatewayService> _logger;

    public PaystackGatewayService(IConfiguration configuration, ILogger<PaystackGatewayService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<InitializePaymentResult> InitializePaymentAsync(
        string email,
        decimal amount,
        string currency,
        string reference,
        string callbackUrl,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var secretKey = _configuration["Paystack:SecretKey"];

        _logger.LogInformation("Initializing Paystack payment: Ref={Reference}, Amount={Amount} {Currency}, Email={Email}",
            reference, amount, currency, email);

        // Production returns direct checkout link from Paystack API
        // For testing / developer environment:
        var checkoutUrl = $"https://checkout.paystack.com/{reference}";
        return Task.FromResult(new InitializePaymentResult(true, reference, checkoutUrl));
    }

    public bool VerifyWebhookSignature(string payload, string signature)
    {
        var secretKey = _configuration["Paystack:SecretKey"] ?? "sk_test_zenith_mock_secret_key";
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var calculatedHex = Convert.ToHexStringLower(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(calculatedHex),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}

