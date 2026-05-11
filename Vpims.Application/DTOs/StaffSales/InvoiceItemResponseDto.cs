namespace Vpims.Application.DTOs.StaffSales;

public class InvoiceItemResponseDto
{
    public Guid PartId { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
