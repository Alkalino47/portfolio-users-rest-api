using System.ComponentModel.DataAnnotations;

namespace portfolio_users_rest_api.Domain.Entities;

public class UserRole
{
    public int UserRoleId { get; set; }

    [Required]
    public string RoleName { get; set; } = null!;
}
