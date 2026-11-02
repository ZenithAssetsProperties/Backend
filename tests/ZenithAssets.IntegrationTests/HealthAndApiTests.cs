using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ZenithAssets.Application.Features.Auth;

namespace ZenithAssets.IntegrationTests;

public class HealthAndApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthAndApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RootEndpoint_ShouldReturnOperationalStatus()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Zenith Assets Properties Transaction API", content);
        Assert.Contains("Operational", content);
    }

    [Fact]
    public async Task HealthLiveEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPayloadIsInvalid()
    {
        // Arrange (invalid password < 8 chars and empty email)
        var invalidCommand = new RegisterUserCommand("", "invalid-email", "", "123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", invalidCommand);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("Validation Failed", problemJson);
    }

    [Fact]
    public async Task Webhook_ShouldReject_WhenSignatureIsMissingOrInvalid()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhook")
        {
            Content = JsonContent.Create(new { @event = "charge.success" })
        };
        request.Headers.Add("x-paystack-signature", "invalid_forged_signature_token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid webhook cryptographic signature", content);
    }
}

