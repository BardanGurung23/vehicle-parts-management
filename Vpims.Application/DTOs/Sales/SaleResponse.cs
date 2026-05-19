using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Sales;

public sealed class SaleResponse
{
    public int SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? VehicleNumber { get; set; }
    public DateTimeOffset SaleDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTimeOffset? DueDate { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<SaleItemResponse> Items { get; set; } = Array.Empty<SaleItemResponse>();
}
