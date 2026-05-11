using Vpims.Application.DTOs.StaffSales;

namespace Vpims.Application.Interfaces;

public interface IStaffSalesService
{
    Task<IReadOnlyCollection<CustomerLookupDto>> SearchCustomersAsync(string? searchTerm, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VehiclePartLookupDto>> SearchPartsAsync(string? searchTerm, CancellationToken cancellationToken);

    Task<InvoiceResponseDto> CreateSaleAsync(CreateSaleRequestDto request, CancellationToken cancellationToken);

    Task<SendInvoiceEmailResponseDto> SendInvoiceEmailAsync(Guid saleId, CancellationToken cancellationToken);
}
