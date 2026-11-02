using ZenithAssets.Domain.Common;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Domain.Entities;

public sealed class User : AuditableEntity<Guid>
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.PublicUser;
    public bool IsEmailVerified { get; private set; }
    public bool IsPhoneVerified { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    // Navigation collections
    public ICollection<Reservation> Reservations { get; private set; } = [];
    public ICollection<Transaction> Transactions { get; private set; } = [];
    public ICollection<Inspection> Inspections { get; private set; } = [];
    public ICollection<DocumentAccessRequest> DocumentAccessRequests { get; private set; } = [];

    private User() { }

    public static User Create(
        string fullName,
        string email,
        string phoneNumber,
        string passwordHash,
        UserRole role = UserRole.Buyer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PhoneNumber = phoneNumber.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            TwoFactorEnabled = false
        };
    }

    public void UpdateProfile(string fullName, string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber.Trim();
    }

    public void VerifyEmail() => IsEmailVerified = true;
    public void VerifyPhone() => IsPhoneVerified = true;

    public void EnableTwoFactor(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        TwoFactorSecret = secret;
        TwoFactorEnabled = true;
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        TwoFactorEnabled = false;
    }

    public void SetRefreshToken(string token, DateTime expiryUtc)
    {
        RefreshToken = token;
        RefreshTokenExpiryUtc = expiryUtc;
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryUtc = null;
    }

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }
}

