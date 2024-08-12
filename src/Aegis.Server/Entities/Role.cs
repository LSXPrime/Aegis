namespace Aegis.Server.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = [];
}