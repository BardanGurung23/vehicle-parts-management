using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Sales;

namespace Vpims.Application.Interfaces.Services;

public interface ISaleService
{
    Task<IReadOnlyList<SaleResponse>> GetCustomerSalesAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleResponse>> GetCustomerSalesByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<SaleResponse> GetSaleByIdAsync(
        int saleId,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);

    Task<SendSaleInvoiceEmailResponse> SendInvoiceEmailAsync(
        int saleId,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);

    Task<SaleResponse> CreateSaleAsync(
        CreateSaleRequest request,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);
}
