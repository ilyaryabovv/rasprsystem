namespace FileService.Storage;

public class SiloOptions
{
    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "siloadmin";
    public string SecretKey { get; set; } = "silo-admin-secret";
    public string Bucket { get; set; } = "reports";
}
