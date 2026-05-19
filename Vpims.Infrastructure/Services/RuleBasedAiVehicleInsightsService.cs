using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.VehicleInsights;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class RuleBasedAiVehicleInsightsService(ICustomerRepository customerRepository) : IAiVehicleInsightsService
{
    private const string LowRisk = "Low";
    private const string MediumRisk = "Medium";
    private const string HighRisk = "High";

    public async Task<VehicleInsightsResponse> GetVehicleInsightsAsync(
        int customerId,
        int vehicleId,
        CancellationToken cancellationToken = default)
    {
        Vehicle vehicle = await customerRepository.GetVehicleByCustomerIdAsync(customerId, vehicleId, cancellationToken)
            ?? throw new NotFoundException($"Vehicle with id {vehicleId} not found.");

        DateTimeOffset generatedAt = DateTimeOffset.UtcNow;
        VehicleInsightContext context = BuildContext(vehicle, generatedAt);

        var insights = new List<VehicleInsightItemResponse>
        {
            BuildServiceInsight(context),
            BuildOilInsight(context),
            BuildBrakeInsight(context),
            BuildBatteryInsight(context),
            BuildTireInsight(context),
        };

        int healthScore = CalculateHealthScore(insights, context);

        return new VehicleInsightsResponse
        {
            VehicleId = vehicle.VehicleId,
            VehicleNumber = vehicle.VehicleNumber,
            Model = vehicle.Model,
            HealthScore = healthScore,
            HealthStatus = GetHealthStatus(healthScore),
            UsagePattern = context.UsagePattern,
            Mileage = vehicle.Mileage,
            ManufactureYear = vehicle.ManufactureYear,
            VehicleAgeYears = context.VehicleAgeYears,
            EstimatedAnnualMileage = context.EstimatedAnnualMileage,
            LastServiceDate = vehicle.LastServiceDate,
            GeneratedAt = generatedAt,
            Insights = insights
                .OrderByDescending(insight => RiskWeight(insight.RiskLevel))
                .ThenBy(insight => insight.Title)
                .ToList(),
        };
    }

    private static VehicleInsightContext BuildContext(Vehicle vehicle, DateTimeOffset generatedAt)
    {
        int? vehicleAgeYears = vehicle.ManufactureYear.HasValue
            ? Math.Max(0, generatedAt.Year - vehicle.ManufactureYear.Value)
            : null;

        int? estimatedAnnualMileage = vehicle.Mileage.HasValue && vehicleAgeYears.HasValue
            ? vehicle.Mileage.Value / Math.Max(1, vehicleAgeYears.Value)
            : null;

        int? daysSinceService = vehicle.LastServiceDate.HasValue
            ? Math.Max(0, (int)(generatedAt.Date - vehicle.LastServiceDate.Value.UtcDateTime.Date).TotalDays)
            : null;

        return new VehicleInsightContext(
            vehicle.Mileage,
            vehicleAgeYears,
            estimatedAnnualMileage,
            daysSinceService,
            DetermineUsagePattern(estimatedAnnualMileage));
    }

    private static VehicleInsightItemResponse BuildServiceInsight(VehicleInsightContext context)
    {
        if (!context.DaysSinceService.HasValue)
        {
            return CreateInsight(
                "SERVICE_HISTORY_MISSING",
                "Service",
                "Service Date Missing",
                "No last service date is recorded, so the system cannot confirm recent maintenance coverage.",
                MediumRisk,
                "Add the last service date or book a general inspection to establish a maintenance baseline.",
                "Recommended within 1 month");
        }

        if (context.DaysSinceService.Value > 365)
        {
            return CreateInsight(
                "SERVICE_OVERDUE",
                "Service",
                "Service Overdue",
                $"The vehicle has gone {context.DaysSinceService.Value} days since the last recorded service.",
                HighRisk,
                "Book a full service inspection and update the service record after completion.",
                "Immediate: within 2 weeks");
        }

        if (context.DaysSinceService.Value > 180)
        {
            return CreateInsight(
                "SERVICE_DUE_SOON",
                "Service",
                "Service Due Soon",
                $"The last recorded service was {context.DaysSinceService.Value} days ago.",
                MediumRisk,
                "Schedule routine maintenance before the service interval becomes overdue.",
                "Recommended within 1 month");
        }

        return CreateInsight(
            "SERVICE_ON_TRACK",
            "Service",
            "Service Schedule On Track",
            $"The last recorded service was {context.DaysSinceService.Value} days ago.",
            LowRisk,
            "Continue following regular maintenance intervals.",
            $"Next routine check in about {Math.Max(30, 180 - context.DaysSinceService.Value)} days");
    }

    private static VehicleInsightItemResponse BuildOilInsight(VehicleInsightContext context)
    {
        int? kilometersToNextOilCheck = context.Mileage.HasValue
            ? 10_000 - context.Mileage.Value % 10_000
            : null;

        if (context.DaysSinceService > 365 || kilometersToNextOilCheck <= 1_000)
        {
            return CreateInsight(
                "ENGINE_OIL_REPLACE",
                "Engine",
                "Engine Oil Replacement Recommended",
                "Mileage and service timing indicate the engine oil interval may be close to or past due.",
                HighRisk,
                "Replace engine oil and oil filter, then update the last service date.",
                kilometersToNextOilCheck.HasValue ? $"Within {Math.Max(500, kilometersToNextOilCheck.Value):N0} km" : "Immediate: within 2 weeks");
        }

        if (context.DaysSinceService > 180 || kilometersToNextOilCheck <= 2_500)
        {
            return CreateInsight(
                "ENGINE_OIL_CHECK",
                "Engine",
                "Engine Oil Check Recommended",
                "The vehicle is approaching a typical oil service interval.",
                MediumRisk,
                "Check oil level and plan oil replacement during the next visit.",
                kilometersToNextOilCheck.HasValue ? $"Within {kilometersToNextOilCheck.Value:N0} km" : "Within 1 month");
        }

        return CreateInsight(
            "ENGINE_OIL_MONITOR",
            "Engine",
            "Engine Oil Looks Stable",
            "The available mileage and service data do not show an urgent oil replacement risk.",
            LowRisk,
            "Monitor oil level monthly and replace at the next scheduled interval.",
            kilometersToNextOilCheck.HasValue ? $"Around {kilometersToNextOilCheck.Value:N0} km remaining" : "Review at next scheduled service");
    }

    private static VehicleInsightItemResponse BuildBrakeInsight(VehicleInsightContext context)
    {
        if (context.Mileage >= 80_000 || (context.EstimatedAnnualMileage >= 25_000 && context.VehicleAgeYears >= 4))
        {
            return CreateInsight(
                "BRAKE_WEAR_HIGH",
                "Brakes",
                "Brake Wear Risk",
                "High mileage or heavy yearly usage increases the chance of brake pad and rotor wear.",
                HighRisk,
                "Inspect brake pads, rotors, and brake fluid before the next long trip.",
                "Within 2,000 km");
        }

        if (context.Mileage >= 40_000 || context.EstimatedAnnualMileage >= 15_000 || context.VehicleAgeYears >= 5)
        {
            return CreateInsight(
                "BRAKE_WEAR_MEDIUM",
                "Brakes",
                "Brake Inspection Recommended",
                "Usage indicators suggest brake components should be checked during upcoming maintenance.",
                MediumRisk,
                "Ask staff to inspect brake pad thickness and braking response.",
                "Within 5,000 km");
        }

        return CreateInsight(
            "BRAKE_WEAR_LOW",
            "Brakes",
            "Brake Wear Risk Low",
            "Current mileage and usage pattern do not indicate urgent brake wear.",
            LowRisk,
            "Continue monitoring for squealing, vibration, or reduced braking response.",
            "Review at next scheduled service");
    }

    private static VehicleInsightItemResponse BuildBatteryInsight(VehicleInsightContext context)
    {
        if (context.VehicleAgeYears >= 5)
        {
            return CreateInsight(
                "BATTERY_HEALTH_HIGH",
                "Electrical",
                "Battery Health Risk",
                "Battery reliability commonly drops as vehicles age beyond five years.",
                HighRisk,
                "Test battery voltage and charging system before symptoms appear.",
                "Within 1 month");
        }

        if (context.VehicleAgeYears >= 3 || context.DaysSinceService > 240)
        {
            return CreateInsight(
                "BATTERY_HEALTH_MEDIUM",
                "Electrical",
                "Battery Inspection Recommended",
                "Vehicle age or service gap suggests the battery should be checked proactively.",
                MediumRisk,
                "Run a battery health test during the next service visit.",
                "Within 3 months");
        }

        return CreateInsight(
            "BATTERY_HEALTH_LOW",
            "Electrical",
            "Battery Health Risk Low",
            "No strong age or service indicators currently point to battery failure risk.",
            LowRisk,
            "Keep terminals clean and test the battery during routine maintenance.",
            "Review at next scheduled service");
    }

    private static VehicleInsightItemResponse BuildTireInsight(VehicleInsightContext context)
    {
        int? kilometersToTireCheck = context.Mileage.HasValue
            ? 20_000 - context.Mileage.Value % 20_000
            : null;

        if (context.Mileage >= 60_000 || (context.VehicleAgeYears >= 5 && context.DaysSinceService > 180))
        {
            return CreateInsight(
                "TIRE_INSPECTION_HIGH",
                "Tires",
                "Tire Inspection Needed",
                "Mileage, age, or service gap indicates higher risk of tread wear or alignment issues.",
                HighRisk,
                "Inspect tread depth, tire pressure, sidewalls, and wheel alignment.",
                "Within 1,000 km");
        }

        if (context.Mileage >= 20_000 || context.VehicleAgeYears >= 3 || kilometersToTireCheck <= 3_000)
        {
            return CreateInsight(
                "TIRE_INSPECTION_MEDIUM",
                "Tires",
                "Tire Inspection Recommended",
                "The vehicle is approaching a sensible tire inspection interval.",
                MediumRisk,
                "Check tread depth, rotate tires if needed, and verify wheel alignment.",
                kilometersToTireCheck.HasValue ? $"Within {Math.Max(1_000, kilometersToTireCheck.Value):N0} km" : "Within 3 months");
        }

        return CreateInsight(
            "TIRE_INSPECTION_LOW",
            "Tires",
            "Tire Condition Looks Stable",
            "Current mileage and age do not indicate urgent tire inspection risk.",
            LowRisk,
            "Maintain correct tire pressure and inspect visually each month.",
            "Review at next scheduled service");
    }

    private static VehicleInsightItemResponse CreateInsight(
        string code,
        string category,
        string title,
        string description,
        string riskLevel,
        string recommendedAction,
        string predictedTimeframe)
    {
        return new VehicleInsightItemResponse
        {
            Code = code,
            Category = category,
            Title = title,
            Description = description,
            RiskLevel = riskLevel,
            RecommendedAction = recommendedAction,
            PredictedTimeframe = predictedTimeframe,
        };
    }

    private static string DetermineUsagePattern(int? estimatedAnnualMileage)
    {
        if (!estimatedAnnualMileage.HasValue)
        {
            return "Unknown";
        }

        if (estimatedAnnualMileage.Value >= 25_000)
        {
            return "High";
        }

        if (estimatedAnnualMileage.Value >= 12_000)
        {
            return "Moderate";
        }

        return "Light";
    }

    private static int CalculateHealthScore(IReadOnlyList<VehicleInsightItemResponse> insights, VehicleInsightContext context)
    {
        int score = 100;

        foreach (VehicleInsightItemResponse insight in insights)
        {
            score -= insight.RiskLevel switch
            {
                HighRisk => 16,
                MediumRisk => 8,
                _ => 2,
            };
        }

        if (context.UsagePattern == "High")
        {
            score -= 4;
        }

        if (!context.Mileage.HasValue || !context.VehicleAgeYears.HasValue || !context.DaysSinceService.HasValue)
        {
            score -= 5;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static string GetHealthStatus(int healthScore)
    {
        if (healthScore >= 80)
        {
            return "Good";
        }

        if (healthScore >= 60)
        {
            return "Moderate";
        }

        return "Critical";
    }

    private static int RiskWeight(string riskLevel)
    {
        return riskLevel switch
        {
            HighRisk => 3,
            MediumRisk => 2,
            _ => 1,
        };
    }

    private sealed record VehicleInsightContext(
        int? Mileage,
        int? VehicleAgeYears,
        int? EstimatedAnnualMileage,
        int? DaysSinceService,
        string UsagePattern);
}