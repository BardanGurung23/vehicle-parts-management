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
        Customer customer = await ResolveCustomerForSaleAsync(request.CustomerId, currentUser, cancellationToken);

        if (request.VehicleId.HasValue)
        {
            await ValidateVehicleOwnershipAsync(customer.CustomerId, request.VehicleId.Value, cancellationToken);
        }

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
        decimal subtotal = 0;
        var saleItems = new List<SaleItem>();
        foreach (var item in request.Items)
        {
            var part = parts.First(p => p.PartId == item.PartId);

            if (part.StockQuantity < item.Quantity)
                throw new AppValidationException($"Insufficient stock for part '{part.PartName}'. Available: {part.StockQuantity}");

            decimal lineTotal = part.UnitPrice * item.Quantity;
            subtotal += lineTotal;
            saleItems.Add(new SaleItem
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                UnitPrice = part.UnitPrice,
                LineTotal = lineTotal,
            });

            // Update stock
            part.StockQuantity -= item.Quantity;
        }

        decimal discountAmount = subtotal > 5000m ? Math.Round(subtotal * 0.10m, 2, MidpointRounding.AwayFromZero) : 0m;
        decimal totalAmount = subtotal - discountAmount;
        DateTimeOffset saleDate = DateTimeOffset.UtcNow;

        var sale = new Sale
        {
            CustomerId = customer.CustomerId,
            VehicleId = request.VehicleId,
            CreatedByUserId = currentUser.UserId,
            InvoiceNumber = BuildInvoiceNumber(saleDate),
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            PaymentStatus = "Paid",
            Notes = request.Notes,
            SaleDate = saleDate,
            Items = saleItems,
        };

        await salesRepository.AddAsync(sale, cancellationToken);
        var createdSale = await salesRepository.GetByIdAsync(sale.SaleId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created sale.");
        return ToSaleResponse(createdSale);
    }

    private async Task<Customer> ResolveCustomerForSaleAsync(
        int? requestedCustomerId,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken)
    {
        if (requestedCustomerId.HasValue)
        {
            if (currentUser.Role is not SystemRoles.Admin and not SystemRoles.Staff)
            {
                throw new AppValidationException("Only staff can create a sale for another customer.");
            }

            return await customerRepository.GetByCustomerIdAsync(requestedCustomerId.Value, cancellationToken)
                ?? throw new NotFoundException($"Customer with id {requestedCustomerId.Value} not found.");
        }

        Customer? customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken);
        if (customer != null)
        {
            return customer;
        }

        if (currentUser.Role is SystemRoles.Admin or SystemRoles.Staff)
        {
            throw new AppValidationException("Select a customer before creating a sale.");
        }

        throw new NotFoundException("Customer profile not found.");
    }

    private async Task ValidateVehicleOwnershipAsync(
        int customerId,
        int vehicleId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Vehicle> vehicles = await customerRepository.GetVehiclesByCustomerIdAsync(customerId, cancellationToken);
        if (vehicles.All(vehicle => vehicle.VehicleId != vehicleId))
        {
            throw new AppValidationException("The selected vehicle does not belong to the chosen customer.");
        }
    }

    private static SaleResponse ToSaleResponse(Sale sale)
    {
        return new SaleResponse
        {
            SaleId = sale.SaleId,
            InvoiceNumber = sale.InvoiceNumber,
            CustomerName = sale.Customer?.FullName ?? "Unknown",
            VehicleNumber = sale.Vehicle?.VehicleNumber,
            SaleDate = sale.SaleDate,
            Subtotal = sale.Subtotal,
            DiscountAmount = sale.DiscountAmount,
            TotalAmount = sale.TotalAmount,
            PaymentStatus = sale.PaymentStatus,
            DueDate = sale.DueDate,
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

    private static string BuildInvoiceNumber(DateTimeOffset saleDate)
    {
        return $"SAL-{saleDate:yyyyMMddHHmmssfff}";
    }
}
