using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Sales;

public sealed class CreateSaleItemRequest
{
    [Required]
    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public sealed class CreateSaleRequest
{
    public int? CustomerId { get; set; }

    public int? VehicleId { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    [MinLength(1)]
    public List<CreateSaleItemRequest> Items { get; set; } = new();

    public string? Notes { get; set; }
}
