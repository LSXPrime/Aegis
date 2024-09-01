namespace Aegis.Server.AspNetCore.DTOs;

public class JwtSettings
{
    public string Secret { get; set; }
    public string Salt { get; set; }
    public int AccessTokenExpirationInDays { get; set; }
    public int RefreshTokenExpirationInDays { get; set; }
}