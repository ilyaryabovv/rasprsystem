namespace ManagerService.Models;

public class ExportSaga
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? Instance { get; set; }
}
