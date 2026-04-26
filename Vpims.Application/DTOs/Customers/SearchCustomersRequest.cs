using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Customers;

public sealed class SearchCustomersRequest
{
    [Range(1, int.MaxValue)]
    public int? CustomerId { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(30)]
    public string? VehicleNumber { get; set; }

    [StringLength(150)]
    public string? Name { get; set; }
}