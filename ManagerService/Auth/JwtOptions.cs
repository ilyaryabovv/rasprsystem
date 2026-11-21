namespace ManagerService.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "manager";
    public string Audience { get; set; } = "manager";
    public string Key { get; set; } = "lab-jwt-signing-key-must-be-32b!";
    public string Username { get; set; } = "lab";
    public string Password { get; set; } = "lab";
}
