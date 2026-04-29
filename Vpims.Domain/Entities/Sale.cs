using System.ComponentModel.DataAnnotations;
using Vpims.Domain.Entities;

namespace Vpims.Domain.Entities;

public sealed class Sale
{
    public int SaleId { get; set; }

    public int CustomerId { get; set; }

    public int? VehicleId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset SaleDate { get; set; }

    public Customer? Customer { get; set; }

    public Vehicle? Vehicle { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
