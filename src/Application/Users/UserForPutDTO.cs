using System.ComponentModel.DataAnnotations;

namespace portfolio_users_rest_api.Application.Users;

public class UserForPutDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "UserName is required")]
    [MaxLength(100, ErrorMessage = "UserName cannot be longer than 100 characters")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "UserPass is required")]
    [MaxLength(100, ErrorMessage = "UserPass cannot be longer than 100 characters")]
    public string UserPass { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "UserRoleId must be greater than 0")]
    public int UserRoleId { get; set; }

    public bool IsActive { get; set; }
}
