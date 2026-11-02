using NSubstitute;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Features.Auth;
using ZenithAssets.Application.Features.Reservations;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Application.UnitTests;

public class AuthAndReservationTests
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDistributedLockService _lockService;
    private readonly IPaymentGatewayService _paymentGateway;

    public AuthAndReservationTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _lockService = Substitute.For<IDistributedLockService>();
        _paymentGateway = Substitute.For<IPaymentGatewayService>();
    }

    [Fact]
    public async Task CreateReservation_ShouldFail_WhenLockCannotBeAcquired()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        // Lock acquisition returns null (simulating concurrent reservation contention)
        _lockService.AcquireLockAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((IAsyncDisposable?)null);

        var handler = new CreateReservationCommandHandler(_context, _currentUser, _lockService, _paymentGateway);
        var command = new CreateReservationCommand(propertyId, "https://zenithassets.ng/callback");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("currently being processed", result.Error);
    }

    [Fact]
    public async Task CreateReservation_ShouldFail_WhenUserNotAuthenticated()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var handler = new CreateReservationCommandHandler(_context, _currentUser, _lockService, _paymentGateway);
        var command = new CreateReservationCommand(Guid.NewGuid(), "https://zenithassets.ng/callback");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("must be authenticated", result.Error);
    }
}

