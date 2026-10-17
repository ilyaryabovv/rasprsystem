namespace ManagerService.Clients;

public interface IFileClient
{
    Task<(byte[]? Content, string? Instance)> Download(string fileName);
}
