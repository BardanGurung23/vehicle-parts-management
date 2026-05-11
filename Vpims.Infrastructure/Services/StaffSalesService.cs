using Microsoft.EntityFrameworkCore;
using Vpims.Application.Common;
using Vpims.Application.DTOs.StaffSales;
using Vpims.Application.Interfaces;
using Vpims.Domain.Models;
using Vpims.Infrastructure.Data;

namespace Vpims.Infrastructure.Services;

public class StaffSalesService(VpimsDbContext dbContext, IEmailService emailService) : IStaffSalesService
{
    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyDiscountRate = 0.10m;

    public async Task<IReadOnlyCollection<CustomerLookupDto>> SearchCustomersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var normalizedSearch = searchTerm?.Trim().ToLowerInvariant();

        return await dbContext.Customers
            .AsNoTracking()
            .Where(customer =>
                string.IsNullOrWhiteSpace(normalizedSearch) ||
                customer.FullName.ToLower().Contains(normalizedSearch) ||
                customer.Email.ToLower().Contains(normalizedSearch))
            .OrderBy(customer => customer.FullName)
            .Take(20)
            .Select(customer => new CustomerLookupDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VehiclePartLookupDto>> SearchPartsAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var normalizedSearch = searchTerm?.Trim().ToLowerInvariant();

        return await dbContext.VehicleParts
            .AsNoTracking()
            .Where(part =>
                string.IsNullOrWhiteSpace(normalizedSearch) ||
                part.Name.ToLower().Contains(normalizedSearch) ||
                part.PartNumber.ToLower().Contains(normalizedSearch))
            .OrderBy(part => part.Name)
            .Take(30)
            .Select(part => new VehiclePartLookupDto
            {
                Id = part.Id,
                PartNumber = part.PartNumber,
                Name = part.Name,
                StockQuantity = part.StockQuantity,
                UnitPrice = part.UnitPrice
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceResponseDto> CreateSaleAsync(CreateSaleRequestDto request, CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(item => item.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("Customer not found.");
        }

        var requestedPartIds = request.Items.Select(item => item.PartId).Distinct().ToList();
        var parts = await dbContext.VehicleParts
            .Where(part => requestedPartIds.Contains(part.Id))
            .ToDictionaryAsync(part => part.Id, cancellationToken);

        if (parts.Count != requestedPartIds.Count)
        {
            throw new NotFoundException("One or more selected vehicle parts were not found.");
        }

        var saleItems = new List<SaleItem>();

        foreach (var requestItem in request.Items)
        {
            var part = parts[requestItem.PartId];

            if (part.StockQuantity < requestItem.Quantity)
            {
                throw new AppValidationException($"Not enough stock for {part.Name}. Available stock: {part.StockQuantity}.");
            }

            var lineTotal = decimal.Round(part.UnitPrice * requestItem.Quantity, 2, MidpointRounding.AwayFromZero);

            saleItems.Add(new SaleItem
            {
                Id = Guid.NewGuid(),
                VehiclePartId = part.Id,
                VehiclePart = part,
                Quantity = requestItem.Quantity,
                UnitPrice = part.UnitPrice,
                LineTotal = lineTotal
            });

            part.StockQuantity -= requestItem.Quantity;
        }

        var subtotal = saleItems.Sum(item => item.LineTotal);
        var discountAmount = subtotal > LoyaltyThreshold
            ? decimal.Round(subtotal * LoyaltyDiscountRate, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var finalTotal = subtotal - discountAmount;

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            CustomerId = customer.Id,
            Customer = customer,
            CreatedAtUtc = DateTime.UtcNow,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            FinalTotal = finalTotal,
            Items = saleItems
        };

        await dbContext.Sales.AddAsync(sale, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapInvoiceResponse(sale, customer);
    }

    public async Task<SendInvoiceEmailResponseDto> SendInvoiceEmailAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var sale = await dbContext.Sales
            .AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.Items)
                .ThenInclude(item => item.VehiclePart)
            .FirstOrDefaultAsync(item => item.Id == saleId, cancellationToken);

        if (sale is null || sale.Customer is null)
        {
            throw new NotFoundException("Invoice not found.");
        }

        if (string.IsNullOrWhiteSpace(sale.Customer.Email))
        {
            throw new AppValidationException("The selected customer does not have an email address.");
        }

        var subject = $"Invoice {sale.InvoiceNumber} from Vehicle Parts Management System";
        var body = BuildInvoiceEmailBody(sale);

        await emailService.SendEmailAsync(sale.Customer.Email, subject, body, cancellationToken);

        return new SendInvoiceEmailResponseDto
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            RecipientEmail = sale.Customer.Email,
            Message = "Invoice email sent successfully."
        };
    }

    private static void ValidateCreateRequest(CreateSaleRequestDto request)
    {
        if (request.CustomerId == Guid.Empty)
        {
            throw new AppValidationException("Customer is required.");
        }

        if (request.Items.Count == 0)
        {
            throw new AppValidationException("At least one vehicle part is required to create an invoice.");
        }

        var duplicatePartIds = request.Items
            .GroupBy(item => item.PartId)
            .Where(group => group.Key != Guid.Empty && group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicatePartIds.Count != 0)
        {
            throw new AppValidationException("Each vehicle part can only appear once in a sale.");
        }

        if (request.Items.Any(item => item.PartId == Guid.Empty))
        {
            throw new AppValidationException("Each line item must include a vehicle part.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            throw new AppValidationException("Each line item quantity must be greater than zero.");
        }
    }

    private static InvoiceResponseDto MapInvoiceResponse(Sale sale, Customer customer)
    {
        return new InvoiceResponseDto
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            CreatedAtUtc = sale.CreatedAtUtc,
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            CustomerEmail = customer.Email,
            Subtotal = sale.Subtotal,
            DiscountAmount = sale.DiscountAmount,
            FinalTotal = sale.FinalTotal,
            Items = sale.Items.Select(item => new InvoiceItemResponseDto
            {
                PartId = item.VehiclePartId,
                PartNumber = item.VehiclePart?.PartNumber ?? string.Empty,
                PartName = item.VehiclePart?.Name ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList()
        };
    }

    private static string BuildInvoiceEmailBody(Sale sale)
    {
        var rows = string.Join(string.Empty, sale.Items.Select(item =>
        {
            var partName = item.VehiclePart?.Name ?? "Unknown Part";
            var partNumber = item.VehiclePart?.PartNumber ?? "N/A";

            return $"""
                    <tr>
                        <td style="padding:8px;border:1px solid #d0d7de;">{partName}</td>
                        <td style="padding:8px;border:1px solid #d0d7de;">{partNumber}</td>
                        <td style="padding:8px;border:1px solid #d0d7de;text-align:right;">{item.Quantity}</td>
                        <td style="padding:8px;border:1px solid #d0d7de;text-align:right;">{item.UnitPrice:F2}</td>
                        <td style="padding:8px;border:1px solid #d0d7de;text-align:right;">{item.LineTotal:F2}</td>
                    </tr>
                    """;
        }));

        return $"""
                <div style="font-family:Segoe UI,Arial,sans-serif;color:#1f2933;">
                    <h2>Invoice {sale.InvoiceNumber}</h2>
                    <p>Dear {sale.Customer?.FullName},</p>
                    <p>Thank you for your purchase. Please find your invoice details below.</p>
                    <table style="border-collapse:collapse;width:100%;margin:16px 0;">
                        <thead>
                            <tr style="background:#f4f6f8;">
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:left;">Part</th>
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:left;">Part Number</th>
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:right;">Qty</th>
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:right;">Unit Price</th>
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:right;">Line Total</th>
                            </tr>
                        </thead>
                        <tbody>{rows}</tbody>
                    </table>
                    <p><strong>Subtotal:</strong> {sale.Subtotal:F2}</p>
                    <p><strong>Discount:</strong> {sale.DiscountAmount:F2}</p>
                    <p><strong>Final Total:</strong> {sale.FinalTotal:F2}</p>
                    <p>Regards,<br />Vehicle Parts Management System</p>
                </div>
                """;
    }
}
