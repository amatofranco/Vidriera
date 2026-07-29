namespace Vidriera.Infrastructure.Auth;

public class JwtOptions
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "Vidriera";
    public string Audience { get; set; } = "Vidriera";
    public int ExpirationMinutes { get; set; } = 480;
}
