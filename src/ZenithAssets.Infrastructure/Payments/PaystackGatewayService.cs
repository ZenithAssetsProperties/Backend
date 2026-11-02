using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZenithAssets.Application.Common.Interfaces;

namespace ZenithAssets.Infrastructure.Payments;

public class PaystackGatewayService : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaystackGatewayService> _logger;

    public PaystackGatewayService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PaystackGatewayService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<InitializePaymentResult> InitializePaymentAsync(
        string email,
        decimal amount,
        string currency,
        string reference,
        string callbackUrl,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var secretKey = _configuration["Paystack:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogError("Paystack SecretKey is not configured. Please supply PAYSTACK__SECRETKEY.");
            return new InitializePaymentResult(false, reference, string.Empty, "Payment gateway configuration is missing.");
        }

        try
        {
            // Paystack amounts are in the subunit of the currency (e.g. Kobo for NGN: amount * 100)
            var amountInSubunits = (long)(amount * 100);

            var payload = new
            {
                email,
                amount = amountInSubunits,
                currency = currency.ToUpperInvariant(),
                reference,
                callback_url = callbackUrl,
                metadata
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.paystack.co/transaction/initialize")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paystack API call failed with status {StatusCode}: {Response}", response.StatusCode, responseJson);
                return new InitializePaymentResult(false, reference, string.Empty, $"Paystack rejected request: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetBoolean();

            if (!status)
            {
                var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown error";
                return new InitializePaymentResult(false, reference, string.Empty, message);
            }

            var data = root.GetProperty("data");
            var authUrl = data.GetProperty("authorization_url").GetString()!;

            _logger.LogInformation("Paystack payment initialized successfully: Ref={Reference}, Url={AuthUrl}", reference, authUrl);
            return new InitializePaymentResult(true, reference, authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while initializing Paystack payment for reference {Reference}", reference);
            return new InitializePaymentResult(false, reference, string.Empty, ex.Message);
        }
    }

    public bool VerifyWebhookSignature(string payload, string signature)
    {
        var secretKey = _configuration["Paystack:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(payload))
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
