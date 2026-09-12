namespace ManagerService.Models;

public class Manager
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int ContractsCount { get; set; }
}
