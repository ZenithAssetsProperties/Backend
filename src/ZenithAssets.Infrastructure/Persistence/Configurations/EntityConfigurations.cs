using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.ValueObjects;

namespace ZenithAssets.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(30);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(250);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(300);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Description).IsRequired();

        // Value Object: PriceNaira
        builder.OwnsOne(p => p.PriceNaira, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price_naira_amount").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("price_naira_currency").HasMaxLength(5).IsRequired();
        });

        // Value Object: PriceUsd
        builder.OwnsOne(p => p.PriceUsd, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price_usd_amount").HasPrecision(18, 2);
            price.Property(m => m.Currency).HasColumnName("price_usd_currency").HasMaxLength(5);
        });

        // Value Object: Address
        builder.OwnsOne(p => p.Address, addr =>
        {
            addr.Property(a => a.State).HasColumnName("address_state").HasMaxLength(100).IsRequired();
            addr.Property(a => a.City).HasColumnName("address_city").HasMaxLength(100).IsRequired();
            addr.Property(a => a.Area).HasColumnName("address_area").HasMaxLength(150).IsRequired();
            addr.Property(a => a.Landmark).HasColumnName("address_landmark").HasMaxLength(200);
            addr.Property(a => a.FullAddress).HasColumnName("address_full").HasMaxLength(500);
        });

        // Value Object: Coordinates
        builder.OwnsOne(p => p.Coordinates, coord =>
        {
            coord.Property(c => c.Latitude).HasColumnName("geo_latitude");
            coord.Property(c => c.Longitude).HasColumnName("geo_longitude");
            coord.Property(c => c.IsPubliclyExposed).HasColumnName("geo_is_public");
        });

        builder.HasMany(p => p.Media)
            .WithOne(m => m.Property)
            .HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Documents)
            .WithOne(d => d.Property)
            .HasForeignKey(d => d.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.PropertyType);
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PaymentReference).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.PaymentReference).IsUnique();

        builder.OwnsOne(r => r.ReservationFee, fee =>
        {
            fee.Property(m => m.Amount).HasColumnName("fee_amount").HasPrecision(18, 2).IsRequired();
            fee.Property(m => m.Currency).HasColumnName("fee_currency").HasMaxLength(5).IsRequired();
        });

        builder.HasOne(r => r.Property)
            .WithMany(p => p.Reservations)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.Status);
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Reference).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Reference).IsUnique();

        builder.OwnsOne(t => t.Amount, amt =>
        {
            amt.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            amt.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(5).IsRequired();
        });

        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
