using Vpims.Application.Common;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Sales;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class SalesService(
    ISalesRepository salesRepository,
    ICustomerRepository customerRepository,
    IPartRepository partRepository) : ISaleService
{
    public async Task<IReadOnlyList<SaleResponse>> GetCustomerSalesAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        var sales = await salesRepository.GetByCustomerIdAsync(customer.CustomerId, cancellationToken);
        return sales.Select(ToSaleResponse).ToList();
    }

    public async Task<IReadOnlyList<SaleResponse>> GetCustomerSalesByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        // Verify customer exists
        var customer = await customerRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (customer == null)
            throw new NotFoundException($"Customer with id {customerId} not found.");

        var sales = await salesRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return sales.Select(ToSaleResponse).ToList();
    }

    public async Task<SaleResponse> GetSaleByIdAsync(
        int saleId,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        Sale sale = await salesRepository.GetByIdAsync(saleId, cancellationToken)
            ?? throw new NotFoundException($"Sale with id {saleId} not found.");

        if (sale.CustomerId != customer.CustomerId)
        {
            throw new AppValidationException("You can only view your own sales.");
        }

        return ToSaleResponse(sale);
    }

    public async Task<SaleResponse> CreateSaleAsync(
        CreateSaleRequest request,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        // Validate parts and get current prices
        var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
        var parts = new List<Part>();
        foreach (var partId in partIds)
        {
            var part = await partRepository.GetByIdAsync(partId, cancellationToken)
                ?? throw new NotFoundException($"Part with id {partId} not found.");
            parts.Add(part);
        }

        // Calculate total amount
        decimal totalAmount = 0;
        var saleItems = new List<SaleItem>();
        foreach (var item in request.Items)
        {
            var part = parts.First(p => p.PartId == item.PartId);

            if (part.StockQuantity < item.Quantity)
                throw new AppValidationException($"Insufficient stock for part '{part.PartName}'. Available: {part.StockQuantity}");

            totalAmount += part.UnitPrice * item.Quantity;
            saleItems.Add(new SaleItem
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                UnitPrice = part.UnitPrice,
            });

            // Update stock
            part.StockQuantity -= item.Quantity;
        }

        var sale = new Sale
        {
            CustomerId = customer.CustomerId,
            VehicleId = request.VehicleId,
            SaleDate = DateTimeOffset.UtcNow,
            TotalAmount = totalAmount,
            Notes = request.Notes,
            Items = saleItems,
        };

        await salesRepository.AddAsync(sale, cancellationToken);
        var createdSale = await salesRepository.GetByIdAsync(sale.SaleId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created sale.");
        return ToSaleResponse(createdSale);
    }

    private static SaleResponse ToSaleResponse(Sale sale)
    {
        return new SaleResponse
        {
            SaleId = sale.SaleId,
            CustomerName = sale.Customer?.FullName ?? "Unknown",
            VehicleNumber = sale.Vehicle?.VehicleNumber,
            SaleDate = sale.SaleDate,
            TotalAmount = sale.TotalAmount,
            Notes = sale.Notes,
            Items = sale.Items.Select(item => new SaleItemResponse
            {
                PartId = item.PartId,
                PartName = item.Part?.PartName ?? "Unknown Part",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            }).ToList(),
        };
    }
}
