using Microclimate.Domain.Alerts;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.Sensors;
using Microclimate.Domain.Thresholds;
using Microclimate.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microclimate.Infrastructure.Persistence.Configurations;

// Відображення сутностей у таблиці за docs/db-schema.md, розділ 3.
// Id сутностей створює домен (Guid v7), тому ValueGeneratedNever.

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms", t =>
        {
            t.HasCheckConstraint("CK_Rooms_Floor", "\"Floor\" BETWEEN -5 AND 200");
            t.HasCheckConstraint("CK_Rooms_AreaM2", "\"AreaM2\" IS NULL OR \"AreaM2\" > 0");
        });
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).HasMaxLength(Room.NameMaxLength).IsRequired();
        builder.Property(r => r.AreaM2).HasPrecision(8, 2);
        builder.HasIndex(r => r.Name).IsUnique();
    }
}

internal sealed class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("Sensors");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.SerialNumber).HasMaxLength(Sensor.SerialNumberMaxLength).IsRequired();
        builder.Property(s => s.Model).HasMaxLength(Sensor.ModelMaxLength);
        builder.Property(s => s.DeviceKeyHash).HasMaxLength(Sensor.DeviceKeyHashMaxLength).IsRequired();
        builder.HasIndex(s => s.SerialNumber).IsUnique();
        builder.HasOne<Room>().WithMany().HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
{
    public void Configure(EntityTypeBuilder<Measurement> builder)
    {
        builder.ToTable("Measurements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Metric).HasColumnName("MetricType");
        builder.HasOne<Sensor>().WithMany().HasForeignKey(m => m.SensorId).OnDelete(DeleteBehavior.Restrict);

        // Основний запит: метрика датчиків кімнати за період.
        builder.HasIndex(m => new { m.SensorId, m.Metric, m.MeasuredAt }).HasDatabaseName("IX_Measurements_Sensor_Metric_Time");
    }
}

internal sealed class ThresholdConfiguration : IEntityTypeConfiguration<Threshold>
{
    public void Configure(EntityTypeBuilder<Threshold> builder)
    {
        builder.ToTable("Thresholds", t => t.HasCheckConstraint(
            "CK_Thresholds_Order",
            "(\"CriticalMin\" IS NOT NULL OR \"WarningMin\" IS NOT NULL OR \"WarningMax\" IS NOT NULL OR \"CriticalMax\" IS NOT NULL)"
            + " AND (\"CriticalMin\" IS NULL OR \"WarningMin\" IS NULL OR \"CriticalMin\" <= \"WarningMin\")"
            + " AND (\"WarningMin\" IS NULL OR \"WarningMax\" IS NULL OR \"WarningMin\" < \"WarningMax\")"
            + " AND (\"WarningMax\" IS NULL OR \"CriticalMax\" IS NULL OR \"WarningMax\" <= \"CriticalMax\")"));
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Metric).HasColumnName("MetricType");
        builder.Ignore(t => t.Limits);
        builder.HasIndex(t => new { t.RoomId, t.Metric }).IsUnique();
        builder.HasOne<Room>().WithMany().HasForeignKey(t => t.RoomId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Metric).HasColumnName("MetricType");
        builder.Property(a => a.Message).HasMaxLength(Alert.MessageMaxLength).IsRequired();
        builder.HasOne<Room>().WithMany().HasForeignKey(a => a.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sensor>().WithMany().HasForeignKey(a => a.SensorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.AcknowledgedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(a => new { a.Status, a.RaisedAt }).HasDatabaseName("IX_Alerts_Status_RaisedAt");
        builder.HasIndex(a => new { a.RoomId, a.Metric, a.Status }).HasDatabaseName("IX_Alerts_Room_Metric_Status");
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(User.PasswordHashMaxLength).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
