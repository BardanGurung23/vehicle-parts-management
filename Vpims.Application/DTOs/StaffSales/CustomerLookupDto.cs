namespace Vpims.Application.DTOs.StaffSales;

public class CustomerLookupDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
