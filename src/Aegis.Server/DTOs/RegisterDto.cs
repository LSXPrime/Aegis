using System.ComponentModel.DataAnnotations;

namespace Aegis.Server.DTOs;

public class RegisterDto
{
    public string Username { get; set; }
    [EmailAddress]
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
}