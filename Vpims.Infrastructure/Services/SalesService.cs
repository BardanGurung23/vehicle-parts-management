using System.Net;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Sales;
using Vpims.Application.Interfaces;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class SalesService(
    ISalesRepository salesRepository,
    ICustomerRepository customerRepository,
    IPartRepository partRepository,
    IEmailService emailService) : ISaleService
{
    private static readonly HashSet<string> AllowedPaymentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Paid",
        "Pending",
        "Credit",
    };

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
        Sale sale = await GetAuthorizedSaleAsync(saleId, currentUser, cancellationToken);

        return ToSaleResponse(sale);
    }

    public async Task<SendSaleInvoiceEmailResponse> SendInvoiceEmailAsync(
        int saleId,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Sale sale = await GetAuthorizedSaleAsync(saleId, currentUser, cancellationToken);

        if (string.IsNullOrWhiteSpace(sale.Customer?.Email))
        {
            throw new AppValidationException("The selected customer does not have an email address.");
        }

        string recipientEmail = sale.Customer.Email.Trim();
        string subject = $"Invoice {sale.InvoiceNumber} from Autonix";
        string htmlBody = BuildInvoiceEmailBody(sale);

        await emailService.SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken);

        return new SendSaleInvoiceEmailResponse
        {
            SaleId = sale.SaleId,
            InvoiceNumber = sale.InvoiceNumber,
            RecipientEmail = recipientEmail,
            Message = "Invoice email sent successfully."
        };
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
        string paymentStatus = ResolvePaymentStatus(request.PaymentStatus);
        DateTimeOffset? dueDate = ResolveDueDate(paymentStatus, request.DueDate);

        var sale = new Sale
        {
            CustomerId = customer.CustomerId,
            VehicleId = request.VehicleId,
            CreatedByUserId = currentUser.UserId,
            InvoiceNumber = BuildInvoiceNumber(saleDate),
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            PaymentStatus = paymentStatus,
            DueDate = dueDate,
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

    private async Task<Sale> GetAuthorizedSaleAsync(
        int saleId,
        UserProfileResponse currentUser,
        CancellationToken cancellationToken)
    {
        Sale sale = await salesRepository.GetByIdAsync(saleId, cancellationToken)
            ?? throw new NotFoundException($"Sale with id {saleId} not found.");

        if (currentUser.Role is SystemRoles.Admin or SystemRoles.Staff)
        {
            return sale;
        }

        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        if (sale.CustomerId != customer.CustomerId)
        {
            throw new AppValidationException("You can only view your own sales.");
        }

        return sale;
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
            CustomerEmail = sale.Customer?.Email,
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

    private static string BuildInvoiceEmailBody(Sale sale)
    {
        string rows = string.Join(string.Empty, sale.Items.Select(item =>
            "<tr>"
            + $"<td style=\"padding:8px;border:1px solid #d0d7de;\">{Encode(item.Part?.PartName ?? "Unknown Part")}</td>"
            + $"<td style=\"padding:8px;border:1px solid #d0d7de;text-align:right;\">{item.Quantity}</td>"
            + $"<td style=\"padding:8px;border:1px solid #d0d7de;text-align:right;\">{item.UnitPrice:F2}</td>"
            + $"<td style=\"padding:8px;border:1px solid #d0d7de;text-align:right;\">{item.LineTotal:F2}</td>"
            + "</tr>"));

        string vehicleMarkup = string.IsNullOrWhiteSpace(sale.Vehicle?.VehicleNumber)
            ? string.Empty
            : $"<p><strong>Vehicle:</strong> {Encode(sale.Vehicle.VehicleNumber)}</p>";
        string dueDateMarkup = sale.DueDate.HasValue
            ? $"<p><strong>Due date:</strong> {sale.DueDate.Value:yyyy-MM-dd}</p>"
            : string.Empty;
        string notesMarkup = string.IsNullOrWhiteSpace(sale.Notes)
            ? string.Empty
            : $"<p><strong>Notes:</strong> {Encode(sale.Notes)}</p>";

        return $@"<div style=""font-family:Segoe UI,Arial,sans-serif;color:#1f2933;line-height:1.5;"">
                <h2>Invoice {Encode(sale.InvoiceNumber)}</h2>
                <p>Dear {Encode(sale.Customer?.FullName ?? "Customer")},</p>
                <p>Thank you for choosing Autonix. Your invoice summary is below.</p>
                {vehicleMarkup}
                <p><strong>Payment status:</strong> {Encode(sale.PaymentStatus)}</p>
                {dueDateMarkup}
                {notesMarkup}
                <table style=""border-collapse:collapse;width:100%;margin:16px 0;"">
                    <thead>
                        <tr style=""background:#f4f6f8;"">
                            <th style=""padding:8px;border:1px solid #d0d7de;text-align:left;"">Part</th>
                            <th style=""padding:8px;border:1px solid #d0d7de;text-align:right;"">Qty</th>
                            <th style=""padding:8px;border:1px solid #d0d7de;text-align:right;"">Unit Price</th>
                            <th style=""padding:8px;border:1px solid #d0d7de;text-align:right;"">Line Total</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>
                <p><strong>Subtotal:</strong> {sale.Subtotal:F2}</p>
                <p><strong>Discount:</strong> {sale.DiscountAmount:F2}</p>
                <p><strong>Invoice total:</strong> {sale.TotalAmount:F2}</p>
                <p>Regards,<br />Autonix</p>
            </div>";
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string ResolvePaymentStatus(string? paymentStatus)
    {
        string resolvedStatus = string.IsNullOrWhiteSpace(paymentStatus)
            ? "Paid"
            : paymentStatus.Trim();

        if (!AllowedPaymentStatuses.Contains(resolvedStatus))
        {
            throw new AppValidationException("Payment status must be Paid, Pending, or Credit.");
        }

        return resolvedStatus;
    }

    private static DateTimeOffset? ResolveDueDate(string paymentStatus, DateTimeOffset? dueDate)
    {
        if (string.Equals(paymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!dueDate.HasValue)
        {
            throw new AppValidationException("A due date is required for pending or credit sales.");
        }

        return dueDate.Value.ToUniversalTime();
    }
}
