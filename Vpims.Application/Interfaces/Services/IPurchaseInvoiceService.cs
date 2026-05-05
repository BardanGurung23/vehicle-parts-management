using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.PurchaseInvoices;

namespace Vpims.Application.Interfaces.Services;

public interface IPurchaseInvoiceService
{
    Task<IReadOnlyList<PurchaseInvoiceResponse>> GetPurchaseInvoicesAsync(
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResponse> GetPurchaseInvoiceByIdAsync(
        int purchaseInvoiceId,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceResponse> CreatePurchaseInvoiceAsync(
        CreatePurchaseInvoiceRequest request,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default);
}