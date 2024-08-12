namespace Aegis.Server.Entities;

public class Activation
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public DateTime ActivationDate { get; init; } = DateTime.UtcNow;
    
    public DateTime LastHeartbeat { get; set; }

    // Navigation properties
    public Guid LicenseId { get; set; }
    public License License { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; }
}