namespace ManagerService.Clients;

public interface IFileClient
{
    Task<byte[]?> Download(string fileName);
}
