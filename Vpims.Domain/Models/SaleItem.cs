namespace Vpims.Domain.Models;

public class SaleItem
{
    public Guid Id { get; set; }

    public Guid SaleId { get; set; }

    public Sale? Sale { get; set; }

    public Guid VehiclePartId { get; set; }

    public VehiclePart? VehiclePart { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
