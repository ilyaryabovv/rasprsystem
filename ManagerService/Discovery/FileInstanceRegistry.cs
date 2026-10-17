namespace ManagerService.Discovery;

public class FileInstanceRegistry
{
    private readonly string _fallback;
    private string[] _addresses = [];

    public FileInstanceRegistry(string fallback)
    {
        _fallback = fallback;
    }

    public IReadOnlyList<string> Addresses => _addresses;

    public void Update(string[] addresses)
    {
        _addresses = addresses;
    }

    public string Next()
    {
        var addresses = _addresses;
        if (addresses.Length == 0)
        {
            return _fallback;
        }

        var address = addresses[Random.Shared.Next(addresses.Length)];
        return $"http://{address}:8080/";
    }
}
