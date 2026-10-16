using ManagerService.Dto;

namespace ManagerService.Clients;

public interface IFileClient
{
    Task<StoredFileDto> Save(byte[] csv, string fileName);
    Task<byte[]?> Download(string fileName);
}
