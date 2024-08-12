namespace Aegis.Server.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public string Role { get; set; } = "User";
    
    // Navigation properties
    public Guid UserId { get; set; }
    public User User { get; set; }
}