using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Dashboard;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class DashboardService(
    IPartRepository partRepository,
    IUserRepository userRepository,
    ICustomerRepository customerRepository) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.Role == SystemRoles.Customer)
        {
            return new DashboardSummaryResponse
            {
                CurrentCustomer = await GetCurrentCustomerAsync(currentUser.UserId, cancellationToken)
            };
        }

        if (currentUser.Role == SystemRoles.Admin)
        {
            IReadOnlyList<Part> parts = await partRepository.GetAllAsync(cancellationToken);
            IReadOnlyList<User> staffUsers = await userRepository.GetStaffUsersAsync(cancellationToken);

            return new DashboardSummaryResponse
            {
                Inventory = BuildInventorySummary(parts),
                Staff = BuildStaffSummary(staffUsers)
            };
        }

        if (currentUser.Role == SystemRoles.Staff)
        {
            IReadOnlyList<Part> parts = await partRepository.GetAllAsync(cancellationToken);

            return new DashboardSummaryResponse
            {
                Inventory = BuildInventorySummary(parts)
            };
        }

        return new DashboardSummaryResponse();
    }

    private async Task<Vpims.Application.DTOs.Customers.CustomerDetailResponse> GetCurrentCustomerAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        return UserMapper.ToCustomerDetailResponse(customer);
    }

    private static DashboardInventorySummaryResponse BuildInventorySummary(IReadOnlyList<Part> parts)
    {
        int outOfStockCount = parts.Count(part => part.StockQuantity == 0);
        int reorderSoonCount = parts.Count(part => part.StockQuantity > 0 && part.StockQuantity <= part.ReorderLevel);
        int healthyCount = parts.Count(part => part.StockQuantity > part.ReorderLevel);

        IReadOnlyList<DashboardInventoryPartResponse> lowStockParts = parts
            .Where(part => part.StockQuantity <= part.ReorderLevel)
            .OrderByDescending(part => part.ReorderLevel - part.StockQuantity)
            .ThenBy(part => part.PartName)
            .Take(5)
            .Select(ToInventoryPartResponse)
            .ToList();

        IReadOnlyList<DashboardInventoryPartResponse> recentParts = parts
            .OrderByDescending(part => part.CreatedAt)
            .ThenBy(part => part.PartName)
            .Take(5)
            .Select(ToInventoryPartResponse)
            .ToList();

        IReadOnlyList<DashboardCountItemResponse> topCategories = parts
            .GroupBy(part => string.IsNullOrWhiteSpace(part.Category?.CategoryName)
                ? "Uncategorized"
                : part.Category!.CategoryName)
            .Select(group => new DashboardCountItemResponse
            {
                Label = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .Take(5)
            .ToList();

        return new DashboardInventorySummaryResponse
        {
            TrackedPartCount = parts.Count,
            LowStockCount = lowStockParts.Count,
            OutOfStockCount = outOfStockCount,
            TotalUnitsOnHand = parts.Sum(part => part.StockQuantity),
            InventoryCost = parts.Sum(part => part.CostPrice * part.StockQuantity),
            StockStatus =
            [
                new DashboardCountItemResponse
                {
                    Label = "Healthy",
                    Count = healthyCount
                },
                new DashboardCountItemResponse
                {
                    Label = "Reorder soon",
                    Count = reorderSoonCount
                },
                new DashboardCountItemResponse
                {
                    Label = "Out of stock",
                    Count = outOfStockCount
                }
            ],
            TopCategories = topCategories,
            LowStockParts = lowStockParts,
            RecentParts = recentParts
        };
    }

    private static DashboardStaffSummaryResponse BuildStaffSummary(IReadOnlyList<User> staffUsers)
    {
        IReadOnlyList<DashboardCountItemResponse> roleBreakdown = staffUsers
            .GroupBy(user => string.IsNullOrWhiteSpace(user.Role?.Name) ? "Unassigned" : user.Role!.Name)
            .Select(group => new DashboardCountItemResponse
            {
                Label = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToList();

        IReadOnlyList<Vpims.Application.DTOs.Users.StaffUserResponse> recentStaff = staffUsers
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.FullName)
            .Take(4)
            .Select(UserMapper.ToStaffResponse)
            .ToList();

        return new DashboardStaffSummaryResponse
        {
            TotalStaffCount = staffUsers.Count,
            ActiveStaffCount = staffUsers.Count(user => user.IsActive),
            RoleBreakdown = roleBreakdown,
            RecentStaff = recentStaff
        };
    }

    private static DashboardInventoryPartResponse ToInventoryPartResponse(Part part)
    {
        return new DashboardInventoryPartResponse
        {
            PartId = part.PartId,
            PartNumber = part.PartNumber,
            PartName = part.PartName,
            StockQuantity = part.StockQuantity,
            ReorderLevel = part.ReorderLevel,
            CategoryName = part.Category?.CategoryName,
            CreatedAt = part.CreatedAt
        };
    }
}