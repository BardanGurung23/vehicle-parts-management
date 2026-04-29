using System.ComponentModel.DataAnnotations;

namespace Vpims.Domain.Entities;

public sealed class PredictiveAlert
{
    public int PredictiveAlertId { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public int? PartId { get; set; }

    [Required]
    public string AlertMessage { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string RiskLevel { get; set; } = "Medium";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Active";

    public DateTimeOffset CreatedAt { get; set; }

    public Customer? Customer { get; set; }

    public Vehicle? Vehicle { get; set; }

    public Part? Part { get; set; }
}