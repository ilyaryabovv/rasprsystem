namespace FileService.Queue;

public class RabbitOptions
{
    public string Host { get; set; } = "localhost";
    public string User { get; set; } = "rabbit";
    public string Password { get; set; } = "rabbit";
    public string Queue { get; set; } = "report-files";
}
