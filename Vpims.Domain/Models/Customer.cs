using System.ComponentModel.DataAnnotations;

namespace Vpims.Domain.Models;

public class Customer
{
    public Guid Id { get; set; }

    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
