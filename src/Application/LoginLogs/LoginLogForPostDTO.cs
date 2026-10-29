using System.ComponentModel.DataAnnotations;

namespace portfolio_users_rest_api.Application.LoginLogs;

public class LoginLogForPostDTO
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required]
    public DateTime? LoginTime { get; set; }

    [Required]
    public bool? Success { get; set; }

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = null!;

    [Required]
    [MaxLength(1024)]
    public string UserAgent { get; set; } = null!;

    [Required(AllowEmptyStrings = true)]
    [MaxLength(2000)]
    public string ErrorMessage { get; set; } = null!;
}
