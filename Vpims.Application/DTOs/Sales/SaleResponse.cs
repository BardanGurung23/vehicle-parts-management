using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Sales;

public sealed class SaleResponse
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
    public DateTimeOffset SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<SaleItemResponse> Items { get; set; } = Array.Empty<SaleItemResponse>();
}
