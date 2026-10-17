namespace FileService.Storage;

public interface IFileStorage
{
    Task EnsureBucket();
    Task Save(string name, Stream content);
    Task<byte[]?> Read(string name);
    Task Delete(string name);
}
