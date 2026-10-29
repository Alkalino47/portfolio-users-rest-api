using System.ComponentModel.DataAnnotations;

namespace portfolio_users_rest_api.Domain.Entities;

public partial class User
{
    public int UserId { get; set; }

    [Required]
    public string UserName { get; set; } = null!;

    [Required]
    public string UserPass { get; set; } = null!;

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [Required]
    public int UserRoleId { get; set; }

    public UserRole? UserRole { get; set; }
}
