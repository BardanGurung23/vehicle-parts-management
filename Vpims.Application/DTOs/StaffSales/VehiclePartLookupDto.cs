namespace Vpims.Application.DTOs.StaffSales;

public class VehiclePartLookupDto
{
    public Guid Id { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public decimal UnitPrice { get; set; }
}
