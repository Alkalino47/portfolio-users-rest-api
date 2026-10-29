namespace portfolio_users_rest_api.Application.Users;

public class UserDTO
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public int UserRoleId { get; set; }

    public string UserRole { get; set; } = null!;

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public bool IsActive { get; set; }
}
