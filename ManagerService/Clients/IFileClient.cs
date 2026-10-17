using ManagerService.Dto;

namespace ManagerService.Clients;

public interface IFileClient
{
    Task<(StoredFileDto File, string? Instance)> Save(byte[] csv, string fileName);
    Task<(byte[]? Content, string? Instance)> Download(string fileName);
}
