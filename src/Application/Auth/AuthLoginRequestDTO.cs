using System.ComponentModel.DataAnnotations;

namespace portfolio_users_rest_api.Application.Auth;

public class AuthLoginRequestDTO
{
    [Required(ErrorMessage = "UserName is required")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "UserPass is required")]
    public string UserPass { get; set; } = null!;
}
