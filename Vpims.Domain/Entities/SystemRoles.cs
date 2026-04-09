namespace Vpims.Domain.Entities;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly string[] StaffAssignableRoles = [Admin, Staff];
}