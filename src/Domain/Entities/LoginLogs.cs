using System.ComponentModel.DataAnnotations;

namespace portfolio_users_rest_api.Domain.Entities;

public partial class LoginLogs
{
  public int LogId { get; set; } // Primary key for the LoginLogs entity
  public string Username { get; set; } = null!; // Username of the user who attempted to log in
  public DateTime LoginTime { get; set; } // Timestamp of when the login attempt occurred
  public bool Success { get; set; } // Indicates if the login attempt was successful
  public string IpAddress { get; set; } = null!; // IP address of the client making the login attempt
  public string UserAgent { get; set; } = null!; // The browser and OS string sent by the client. Helps identify automated script bots versus human browsers.
  public string ErrorMessage { get; set; } = null!; // Error message if the login attempt failed

}