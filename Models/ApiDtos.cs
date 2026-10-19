namespace MiotoCarRental.Models;

public sealed record PublicVehicleDto(
    int Id,
    string Brand,
    string Model,
    int YearManufacture,
    string Category,
    int CategoryId,
    int BatteryRangeKm,
    int MaxSpeedKmH,
    string BatteryType,
    bool RequiresLicenseA1,
    decimal PricePerDay,
    string City,
    string District,
    bool InstantBooking,
    string? ThumbnailUrl,
    int LimitKmPerDay);

public sealed record PublicVehicleDetailsDto(
    int Id,
    string Brand,
    string Model,
    int YearManufacture,
    string Category,
    int BatteryRangeKm,
    int MaxSpeedKmH,
    string BatteryType,
    bool RequiresLicenseA1,
    decimal PricePerDay,
    string City,
    string District,
    bool InstantBooking,
    string? ThumbnailUrl,
    string? Description,
    string HostName);

public sealed record PublicCategoryStatsDto(
    int Economy,
    int MidRange,
    int Touring,
    int SportPremium);

public sealed record PublicStatsDto(
    string Architecture,
    int TotalVehicles,
    int TotalUsers,
    int TotalBookings,
    PublicCategoryStatsDto Categories);

public sealed record BookingSummaryDto(
    string VehicleName,
    DateTime PickupDate,
    DateTime ReturnDate,
    int TotalDays,
    string Status);
