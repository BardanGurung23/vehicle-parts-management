using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.PurchaseInvoices;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class PurchaseInvoiceService(
    IPurchaseInvoiceRepository purchaseInvoiceRepository,
    IVendorRepository vendorRepository,
    IPartRepository partRepository) : IPurchaseInvoiceService
{
    public async Task<IReadOnlyList<PurchaseInvoiceResponse>> GetPurchaseInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await purchaseInvoiceRepository.GetAllAsync(cancellationToken);
        return invoices.Select(ToPurchaseInvoiceResponse).ToList();
    }

    public async Task<PurchaseInvoiceResponse> GetPurchaseInvoiceByIdAsync(int purchaseInvoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await purchaseInvoiceRepository.GetByIdAsync(purchaseInvoiceId, cancellationToken)
            ?? throw new NotFoundException($"Purchase invoice with id {purchaseInvoiceId} not found.");

        return ToPurchaseInvoiceResponse(invoice);
    }

    public async Task<PurchaseInvoiceResponse> CreatePurchaseInvoiceAsync(
        CreatePurchaseInvoiceRequest request,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new AppValidationException("At least one purchase invoice item is required.");
        }

        Vendor vendor = await vendorRepository.GetByIdAsync(request.VendorId, cancellationToken)
            ?? throw new NotFoundException($"Vendor with id {request.VendorId} not found.");

        List<int> partIds = request.Items.Select(item => item.PartId).Distinct().ToList();
        var parts = new Dictionary<int, Part>(partIds.Count);

        foreach (int partId in partIds)
        {
            Part part = await partRepository.GetByIdAsync(partId, cancellationToken)
                ?? throw new NotFoundException($"Part with id {partId} not found.");
            parts[partId] = part;
        }

        DateTimeOffset invoiceDate = DateTimeOffset.UtcNow;
        var items = new List<PurchaseInvoiceItem>(request.Items.Count);
        decimal totalAmount = 0m;

        foreach (CreatePurchaseInvoiceItemRequest item in request.Items)
        {
            Part part = parts[item.PartId];
            decimal lineTotal = Math.Round(item.UnitCost * item.Quantity, 2, MidpointRounding.AwayFromZero);
            totalAmount += lineTotal;

            part.StockQuantity += item.Quantity;
            part.CostPrice = item.UnitCost;
            part.VendorId = vendor.VendorId;

            items.Add(new PurchaseInvoiceItem
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LineTotal = lineTotal,
            });
        }

        var purchaseInvoice = new PurchaseInvoice
        {
            VendorId = vendor.VendorId,
            CreatedByUserId = currentUser.UserId,
            InvoiceNumber = BuildInvoiceNumber(invoiceDate),
            InvoiceDate = invoiceDate,
            TotalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Completed" : request.Status.Trim(),
            Items = items,
        };

        PurchaseInvoice created = await purchaseInvoiceRepository.CreateAsync(purchaseInvoice, cancellationToken);
        return ToPurchaseInvoiceResponse(created);
    }

    private static PurchaseInvoiceResponse ToPurchaseInvoiceResponse(PurchaseInvoice purchaseInvoice)
    {
        return new PurchaseInvoiceResponse
        {
            PurchaseInvoiceId = purchaseInvoice.PurchaseInvoiceId,
            VendorId = purchaseInvoice.VendorId,
            VendorName = purchaseInvoice.Vendor?.VendorName ?? "Unknown vendor",
            CreatedByUserId = purchaseInvoice.CreatedByUserId,
            CreatedByName = purchaseInvoice.CreatedByUser?.FullName ?? "Unknown user",
            InvoiceNumber = purchaseInvoice.InvoiceNumber,
            InvoiceDate = purchaseInvoice.InvoiceDate,
            TotalAmount = purchaseInvoice.TotalAmount,
            Status = purchaseInvoice.Status,
            Items = purchaseInvoice.Items.Select(item => new PurchaseInvoiceItemResponse
            {
                PurchaseInvoiceItemId = item.PurchaseInvoiceItemId,
                PartId = item.PartId,
                PartName = item.Part?.PartName ?? "Unknown part",
                PartNumber = item.Part?.PartNumber ?? string.Empty,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LineTotal = item.LineTotal,
            }).ToList()
        };
    }

    private static string BuildInvoiceNumber(DateTimeOffset invoiceDate)
    {
        return $"PUR-{invoiceDate:yyyyMMddHHmmssfff}";
    }
}