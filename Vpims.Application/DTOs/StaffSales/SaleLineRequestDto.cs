using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.StaffSales;

public class SaleLineRequestDto
{
    [Required]
    public Guid PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
