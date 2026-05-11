using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.StaffSales;

public class CreateSaleRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [MinLength(1)]
    public List<SaleLineRequestDto> Items { get; set; } = [];
}
