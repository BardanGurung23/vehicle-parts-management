using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Services;

public sealed class DemoDataSeeder(
    AppDbContext dbContext,
    PasswordHasher<User> passwordHasher)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, int> roleMap = await EnsureRolesAsync(cancellationToken);
        IReadOnlyDictionary<string, int> categoryMap = await EnsurePartCategoriesAsync(cancellationToken);

        DemoSeedUsers users = await EnsureUsersAsync(roleMap, cancellationToken);
        IReadOnlyDictionary<string, Vendor> vendors = await EnsureVendorsAsync(cancellationToken);
        IReadOnlyDictionary<string, Part> parts = await EnsurePartsAsync(categoryMap, vendors, cancellationToken);
        IReadOnlyDictionary<string, Vehicle> vehicles = await EnsureVehiclesAsync(users, cancellationToken);

        await EnsurePurchaseInvoicesAsync(users, vendors, parts, cancellationToken);
        await EnsureSalesAsync(users, vehicles, parts, cancellationToken);
        await EnsureAppointmentsAndReviewsAsync(users, vehicles, cancellationToken);
        await EnsurePartRequestsAsync(users, vehicles, cancellationToken);
        await EnsurePredictiveAlertsAsync(users, vehicles, parts, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, int>> EnsureRolesAsync(CancellationToken cancellationToken)
    {
        (string Name, string Description)[] roles =
        [
            (SystemRoles.Admin, "System administrator with full access"),
            (SystemRoles.Staff, "Staff user handling customers, sales, and invoices"),
            (SystemRoles.Customer, "Customer self-service account")
        ];

        foreach ((string name, string description) in roles)
        {
            Role? existing = await dbContext.Roles.FirstOrDefaultAsync(role => role.Name == name, cancellationToken);
            if (existing is null)
            {
                dbContext.Roles.Add(new Role { Name = name, Description = description });
            }
            else
            {
                existing.Description = description;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Roles
            .AsNoTracking()
            .ToDictionaryAsync(role => role.Name, role => role.RoleId, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, int>> EnsurePartCategoriesAsync(CancellationToken cancellationToken)
    {
        (string Name, string Description)[] categories =
        [
            ("Engine", "Filters, belts, sensors, and engine service parts."),
            ("Brakes", "Pads, discs, cylinders, and brake hardware."),
            ("Suspension", "Shocks, bushings, arms, and alignment parts."),
            ("Electrical", "Batteries, lights, relays, and charging parts."),
            ("Body Parts", "Mirrors, bumpers, lamps, and trim."),
            ("Fluids", "Lubricants, coolants, and service fluids.")
        ];

        foreach ((string name, string description) in categories)
        {
            PartCategory? existing = await dbContext.PartCategories.FirstOrDefaultAsync(category => category.CategoryName == name, cancellationToken);
            if (existing is null)
            {
                dbContext.PartCategories.Add(new PartCategory { CategoryName = name, Description = description });
            }
            else
            {
                existing.Description = description;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.PartCategories
            .AsNoTracking()
            .ToDictionaryAsync(category => category.CategoryName, category => category.PartCategoryId, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task<DemoSeedUsers> EnsureUsersAsync(IReadOnlyDictionary<string, int> roleMap, CancellationToken cancellationToken)
    {
        User admin1 = await UpsertStaffAsync("Demo Admin One", "demo.admin1@autonix.local", "+9779801001001", "DemoPass123!", roleMap[SystemRoles.Admin], cancellationToken);
        User admin2 = await UpsertStaffAsync("Demo Admin Two", "demo.admin2@autonix.local", "+9779801001002", "DemoPass123!", roleMap[SystemRoles.Admin], cancellationToken);
        User staff1 = await UpsertStaffAsync("Demo Staff One", "demo.staff1@autonix.local", "+9779802002001", "DemoPass123!", roleMap[SystemRoles.Staff], cancellationToken);
        User staff2 = await UpsertStaffAsync("Demo Staff Two", "demo.staff2@autonix.local", "+9779802002002", "DemoPass123!", roleMap[SystemRoles.Staff], cancellationToken);

        DemoSeedCustomer customer1 = await UpsertCustomerAsync("Demo Customer One", "demo.customer1@autonix.local", "+9779803003001", "Kathmandu, Nepal", "DemoPass123!", roleMap[SystemRoles.Customer], cancellationToken);
        DemoSeedCustomer customer2 = await UpsertCustomerAsync("Demo Customer Two", "demo.customer2@autonix.local", "+9779803003002", "Pokhara, Nepal", "DemoPass123!", roleMap[SystemRoles.Customer], cancellationToken);
        DemoSeedCustomer customer3 = await UpsertCustomerAsync("Demo Customer Three", "demo.customer3@autonix.local", "+9779803003003", "Lalitpur, Nepal", "DemoPass123!", roleMap[SystemRoles.Customer], cancellationToken);

        return new DemoSeedUsers(admin1, admin2, staff1, staff2, customer1, customer2, customer3);
    }

    private async Task<User> UpsertStaffAsync(
        string fullName,
        string email,
        string phoneNumber,
        string password,
        int roleId,
        CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users.FirstOrDefaultAsync(existing => existing.Email == email, cancellationToken);
        if (user is null)
        {
            user = new User { Email = email, CreatedAt = DateTimeOffset.UtcNow };
            dbContext.Users.Add(user);
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.RoleId = roleId;
        user.IsActive = true;
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task<DemoSeedCustomer> UpsertCustomerAsync(
        string fullName,
        string email,
        string phoneNumber,
        string address,
        string password,
        int roleId,
        CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users
            .Include(existing => existing.Customer)
            .FirstOrDefaultAsync(existing => existing.Email == email, cancellationToken);

        if (user is null)
        {
            user = new User { Email = email, CreatedAt = DateTimeOffset.UtcNow };
            dbContext.Users.Add(user);
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.RoleId = roleId;
        user.IsActive = true;
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        Customer? customer = user.Customer;
        if (customer is null)
        {
            customer = await dbContext.Customers.FirstOrDefaultAsync(existing => existing.Email == email, cancellationToken);
            if (customer is null)
            {
                customer = new Customer { RegisteredAt = DateTimeOffset.UtcNow };
                dbContext.Customers.Add(customer);
            }
        }

        customer.User = user;
        customer.FullName = fullName;
        customer.PhoneNumber = phoneNumber;
        customer.Email = email;
        customer.Address = address;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DemoSeedCustomer(user, customer);
    }

    private async Task<IReadOnlyDictionary<string, Vendor>> EnsureVendorsAsync(CancellationToken cancellationToken)
    {
        Vendor[] vendors =
        [
            new() { VendorName = "Everest Auto Supplies", ContactPerson = "Nima Sherpa", PhoneNumber = "+97714100101", Email = "sales@everest-auto.local", Address = "Balaju, Kathmandu" },
            new() { VendorName = "Himal Parts House", ContactPerson = "Rita Tamang", PhoneNumber = "+97714100102", Email = "orders@himal-parts.local", Address = "Putalisadak, Kathmandu" },
            new() { VendorName = "Valley Brake Center", ContactPerson = "Sujan KC", PhoneNumber = "+97714100103", Email = "supply@valley-brake.local", Address = "Lalitpur, Nepal" }
        ];

        foreach (Vendor seedVendor in vendors)
        {
            Vendor? existing = await dbContext.Vendors.FirstOrDefaultAsync(vendor => vendor.VendorName == seedVendor.VendorName, cancellationToken);
            if (existing is null)
            {
                seedVendor.CreatedAt = DateTimeOffset.UtcNow;
                dbContext.Vendors.Add(seedVendor);
            }
            else
            {
                existing.ContactPerson = seedVendor.ContactPerson;
                existing.PhoneNumber = seedVendor.PhoneNumber;
                existing.Email = seedVendor.Email;
                existing.Address = seedVendor.Address;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Vendors
            .AsNoTracking()
            .ToDictionaryAsync(vendor => vendor.VendorName, vendor => vendor, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, Part>> EnsurePartsAsync(
        IReadOnlyDictionary<string, int> categoryMap,
        IReadOnlyDictionary<string, Vendor> vendors,
        CancellationToken cancellationToken)
    {
        Part[] parts =
        [
            new() { PartNumber = "BRK-PAD-001", PartName = "Premium Brake Pad Set", PartCategoryId = categoryMap["Brakes"], VendorId = vendors["Valley Brake Center"].VendorId, UnitPrice = 3200m, CostPrice = 2500m, StockQuantity = 8, ReorderLevel = 10, ImageUrl = "/catalog/photos/brake-pad.jpg", Description = "Low-dust ceramic front brake pad set tuned for daily-driven sedan and hatchback platforms." },
            new() { PartNumber = "BRK-DSC-002", PartName = "Ventilated Brake Disc Rotor", PartCategoryId = categoryMap["Brakes"], VendorId = vendors["Valley Brake Center"].VendorId, UnitPrice = 4600m, CostPrice = 3600m, StockQuantity = 11, ReorderLevel = 6, ImageUrl = "/catalog/photos/brake-rotor.jpg", Description = "Front ventilated rotor with anti-rust coating for heat-stable braking under mixed city and highway use." },
            new() { PartNumber = "ENG-FLT-001", PartName = "Engine Oil Filter", PartCategoryId = categoryMap["Engine"], VendorId = vendors["Everest Auto Supplies"].VendorId, UnitPrice = 850m, CostPrice = 540m, StockQuantity = 32, ReorderLevel = 12, ImageUrl = "/catalog/photos/oil-filter.jpg", Description = "Spin-on oil filter with synthetic blend media for routine service intervals up to 10,000 km." },
            new() { PartNumber = "ENG-BLT-002", PartName = "Serpentine Belt", PartCategoryId = categoryMap["Engine"], VendorId = vendors["Everest Auto Supplies"].VendorId, UnitPrice = 2100m, CostPrice = 1450m, StockQuantity = 15, ReorderLevel = 7, ImageUrl = "/catalog/photos/serpentine-belt.jpg", Description = "Ribbed accessory drive belt for common four-cylinder service jobs with quiet-running EPDM compound." },
            new() { PartNumber = "ELC-BAT-001", PartName = "12V Maintenance-Free Battery", PartCategoryId = categoryMap["Electrical"], VendorId = vendors["Himal Parts House"].VendorId, UnitPrice = 9800m, CostPrice = 7600m, StockQuantity = 5, ReorderLevel = 6, ImageUrl = "/catalog/photos/battery.jpg", Description = "Long-life 12V battery with high cold-crank reserve for passenger vehicles and compact SUVs." },
            new() { PartNumber = "ELC-PLG-002", PartName = "Iridium Spark Plug Set", PartCategoryId = categoryMap["Electrical"], VendorId = vendors["Himal Parts House"].VendorId, UnitPrice = 2800m, CostPrice = 1800m, StockQuantity = 19, ReorderLevel = 8, ImageUrl = "/catalog/photos/spark-plug.jpg", Description = "Four-plug service kit with iridium tips for smoother starts, stronger ignition, and longer replacement cycles." },
            new() { PartNumber = "SUS-SHK-001", PartName = "Rear Shock Absorber", PartCategoryId = categoryMap["Suspension"], VendorId = vendors["Everest Auto Supplies"].VendorId, UnitPrice = 4100m, CostPrice = 3000m, StockQuantity = 14, ReorderLevel = 8, ImageUrl = "/catalog/photos/shock-absorber.jpg", Description = "Gas-charged rear damper that restores ride control and limits body roll over uneven roads." },
            new() { PartNumber = "SUS-BSH-002", PartName = "Control Arm Bushing Kit", PartCategoryId = categoryMap["Suspension"], VendorId = vendors["Everest Auto Supplies"].VendorId, UnitPrice = 1900m, CostPrice = 1250m, StockQuantity = 22, ReorderLevel = 10, ImageUrl = "/catalog/photos/bushing-kit.jpg", Description = "Front suspension bushing set for steering stability, quieter operation, and reduced tyre wear." },
            new() { PartNumber = "FLD-CLT-001", PartName = "Engine Coolant", PartCategoryId = categoryMap["Fluids"], VendorId = vendors["Himal Parts House"].VendorId, UnitPrice = 1200m, CostPrice = 780m, StockQuantity = 18, ReorderLevel = 10, ImageUrl = "/catalog/photos/coolant.jpg", Description = "Pre-mixed long-life coolant for radiator flushes and top-ups across daily service appointments." },
            new() { PartNumber = "FLD-OIL-002", PartName = "Fully Synthetic Engine Oil 5W-30", PartCategoryId = categoryMap["Fluids"], VendorId = vendors["Himal Parts House"].VendorId, UnitPrice = 3400m, CostPrice = 2500m, StockQuantity = 24, ReorderLevel = 12, ImageUrl = "/catalog/photos/engine-oil.jpg", Description = "Four-litre synthetic oil pack for modern petrol engines needing stable cold-start and long-drain performance." },
            new() { PartNumber = "BDY-LMP-001", PartName = "LED Headlamp Assembly", PartCategoryId = categoryMap["Body Parts"], VendorId = vendors["Valley Brake Center"].VendorId, UnitPrice = 8800m, CostPrice = 6900m, StockQuantity = 7, ReorderLevel = 4, ImageUrl = "/catalog/photos/headlamp.jpg", Description = "Driver-side LED headlamp assembly with clear lens and sealed housing for collision repair jobs." },
            new() { PartNumber = "BDY-MRR-002", PartName = "Side Mirror Glass Kit", PartCategoryId = categoryMap["Body Parts"], VendorId = vendors["Valley Brake Center"].VendorId, UnitPrice = 1600m, CostPrice = 980m, StockQuantity = 13, ReorderLevel = 6, ImageUrl = "/catalog/photos/mirror-glass.jpg", Description = "Heated mirror glass replacement kit with backing plate for quick exterior refresh work." }
        ];

        foreach (Part seedPart in parts)
        {
            Part? existing = await dbContext.Parts.FirstOrDefaultAsync(part => part.PartNumber == seedPart.PartNumber, cancellationToken);
            if (existing is null)
            {
                seedPart.CreatedAt = DateTimeOffset.UtcNow;
                dbContext.Parts.Add(seedPart);
            }
            else
            {
                existing.PartName = seedPart.PartName;
                existing.PartCategoryId = seedPart.PartCategoryId;
                existing.VendorId = seedPart.VendorId;
                existing.UnitPrice = seedPart.UnitPrice;
                existing.CostPrice = seedPart.CostPrice;
                existing.StockQuantity = seedPart.StockQuantity;
                existing.ReorderLevel = seedPart.ReorderLevel;
                existing.ImageUrl = seedPart.ImageUrl;
                existing.Description = seedPart.Description;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Parts
            .AsNoTracking()
            .ToDictionaryAsync(part => part.PartNumber, part => part, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, Vehicle>> EnsureVehiclesAsync(DemoSeedUsers users, CancellationToken cancellationToken)
    {
        Vehicle customerOneVehicle = await UpsertVehicleAsync(users.Customer1.Customer.CustomerId, "BA 2 PA 3001", "Toyota Corolla", cancellationToken);
        Vehicle customerTwoVehicle = await UpsertVehicleAsync(users.Customer2.Customer.CustomerId, "GA 3 CHA 2002", "Honda City", cancellationToken);
        Vehicle customerThreeVehicle = await UpsertVehicleAsync(users.Customer3.Customer.CustomerId, "LU 1 KHA 7777", "Hyundai i20", cancellationToken);

        return new Dictionary<string, Vehicle>(StringComparer.OrdinalIgnoreCase)
        {
            [users.Customer1.User.Email] = customerOneVehicle,
            [users.Customer2.User.Email] = customerTwoVehicle,
            [users.Customer3.User.Email] = customerThreeVehicle,
        };
    }

    private async Task<Vehicle> UpsertVehicleAsync(int customerId, string vehicleNumber, string model, CancellationToken cancellationToken)
    {
        Vehicle? vehicle = await dbContext.Vehicles.FirstOrDefaultAsync(existing => existing.VehicleNumber == vehicleNumber, cancellationToken);
        if (vehicle is null)
        {
            vehicle = new Vehicle { VehicleNumber = vehicleNumber, CreatedAt = DateTimeOffset.UtcNow };
            dbContext.Vehicles.Add(vehicle);
        }

        vehicle.CustomerId = customerId;
        vehicle.Model = model;

        await dbContext.SaveChangesAsync(cancellationToken);
        return vehicle;
    }

    private async Task EnsurePurchaseInvoicesAsync(
        DemoSeedUsers users,
        IReadOnlyDictionary<string, Vendor> vendors,
        IReadOnlyDictionary<string, Part> parts,
        CancellationToken cancellationToken)
    {
        PurchaseInvoice? invoice = await dbContext.PurchaseInvoices
            .Include(existing => existing.Items)
            .FirstOrDefaultAsync(existing => existing.InvoiceNumber == "PINV-DEMO-0001", cancellationToken);

        if (invoice is null)
        {
            invoice = new PurchaseInvoice
            {
                VendorId = vendors["Everest Auto Supplies"].VendorId,
                CreatedByUserId = users.Admin1.UserId,
                InvoiceNumber = "PINV-DEMO-0001",
                InvoiceDate = DateTimeOffset.UtcNow.AddDays(-10),
                Status = "Completed",
            };

            invoice.Items.Add(new PurchaseInvoiceItem
            {
                PartId = parts["ENG-FLT-001"].PartId,
                Quantity = 20,
                UnitCost = 540m,
                LineTotal = 10800m,
            });

            invoice.Items.Add(new PurchaseInvoiceItem
            {
                PartId = parts["SUS-SHK-001"].PartId,
                Quantity = 6,
                UnitCost = 3000m,
                LineTotal = 18000m,
            });

            invoice.TotalAmount = invoice.Items.Sum(item => item.LineTotal);
            dbContext.PurchaseInvoices.Add(invoice);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureSalesAsync(
        DemoSeedUsers users,
        IReadOnlyDictionary<string, Vehicle> vehicles,
        IReadOnlyDictionary<string, Part> parts,
        CancellationToken cancellationToken)
    {
        Sale? sale = await dbContext.Sales
            .Include(existing => existing.Items)
            .FirstOrDefaultAsync(existing => existing.InvoiceNumber == "SAL-DEMO-0001", cancellationToken);

        if (sale is not null)
        {
            return;
        }

        sale = new Sale
        {
            CustomerId = users.Customer1.Customer.CustomerId,
            VehicleId = vehicles[users.Customer1.User.Email].VehicleId,
            CreatedByUserId = users.Staff1.UserId,
            InvoiceNumber = "SAL-DEMO-0001",
            SaleDate = DateTimeOffset.UtcNow.AddDays(-2),
            PaymentStatus = "Paid",
            Notes = "Seeded loyalty sale",
        };

        sale.Items.Add(new SaleItem
        {
            PartId = parts["BRK-PAD-001"].PartId,
            Quantity = 2,
            UnitPrice = 3200m,
            LineTotal = 6400m,
        });

        sale.Subtotal = sale.Items.Sum(item => item.LineTotal);
        sale.DiscountAmount = Math.Round(sale.Subtotal * 0.10m, 2, MidpointRounding.AwayFromZero);
        sale.TotalAmount = sale.Subtotal - sale.DiscountAmount;

        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAppointmentsAndReviewsAsync(
        DemoSeedUsers users,
        IReadOnlyDictionary<string, Vehicle> vehicles,
        CancellationToken cancellationToken)
    {
        Appointment? completedAppointment = await dbContext.Appointments
            .Include(appointment => appointment.Review)
            .FirstOrDefaultAsync(appointment => appointment.CustomerId == users.Customer1.Customer.CustomerId && appointment.ServiceType == "Brake Inspection", cancellationToken);

        if (completedAppointment is null)
        {
            completedAppointment = new Appointment
            {
                CustomerId = users.Customer1.Customer.CustomerId,
                VehicleId = vehicles[users.Customer1.User.Email].VehicleId,
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(-5),
                ServiceType = "Brake Inspection",
                Status = "Completed",
                Notes = "Seeded completed appointment",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-7),
            };
            dbContext.Appointments.Add(completedAppointment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (completedAppointment.Review is null)
        {
            dbContext.ServiceReviews.Add(new ServiceReview
            {
                AppointmentId = completedAppointment.AppointmentId,
                CustomerId = users.Customer1.Customer.CustomerId,
                Rating = 5,
                Comment = "Quick service and helpful staff.",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-4),
            });
        }

        bool hasPendingAppointment = await dbContext.Appointments.AnyAsync(
            appointment => appointment.CustomerId == users.Customer2.Customer.CustomerId && appointment.ServiceType == "Oil Change",
            cancellationToken);

        if (!hasPendingAppointment)
        {
            dbContext.Appointments.Add(new Appointment
            {
                CustomerId = users.Customer2.Customer.CustomerId,
                VehicleId = vehicles[users.Customer2.User.Email].VehicleId,
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(3),
                ServiceType = "Oil Change",
                Status = "Pending",
                Notes = "Seeded pending appointment",
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePartRequestsAsync(
        DemoSeedUsers users,
        IReadOnlyDictionary<string, Vehicle> vehicles,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.PartRequests.AnyAsync(
            request => request.CustomerId == users.Customer3.Customer.CustomerId && request.RequestedPartName == "A/C Compressor",
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.PartRequests.Add(new PartRequest
        {
            CustomerId = users.Customer3.Customer.CustomerId,
            VehicleId = vehicles[users.Customer3.User.Email].VehicleId,
            RequestedPartName = "A/C Compressor",
            RequestDetails = "Required for 2017 hatchback cooling issue.",
            Status = "Pending",
            RequestedAt = DateTimeOffset.UtcNow.AddDays(-1),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePredictiveAlertsAsync(
        DemoSeedUsers users,
        IReadOnlyDictionary<string, Vehicle> vehicles,
        IReadOnlyDictionary<string, Part> parts,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.PredictiveAlerts.AnyAsync(
            alert => alert.CustomerId == users.Customer1.Customer.CustomerId && alert.PartId == parts["BRK-PAD-001"].PartId,
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.PredictiveAlerts.Add(new PredictiveAlert
        {
            CustomerId = users.Customer1.Customer.CustomerId,
            VehicleId = vehicles[users.Customer1.User.Email].VehicleId,
            PartId = parts["BRK-PAD-001"].PartId,
            AlertMessage = "Brake pad wear indicates replacement may be needed soon.",
            RiskLevel = "High",
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-6),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public sealed record DemoSeedCustomer(User User, Customer Customer);

    public sealed record DemoSeedUsers(
        User Admin1,
        User Admin2,
        User Staff1,
        User Staff2,
        DemoSeedCustomer Customer1,
        DemoSeedCustomer Customer2,
        DemoSeedCustomer Customer3);
}