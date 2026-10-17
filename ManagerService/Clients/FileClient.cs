using ManagerService.Discovery;

namespace ManagerService.Clients;

public class FileClient : IFileClient
{
    private readonly HttpClient _http;
    private readonly FileInstanceRegistry _registry;

    public FileClient(HttpClient http, FileInstanceRegistry registry)
    {
        _http = http;
        _registry = registry;
    }

    public async Task<(byte[]? Content, string? Instance)> Download(string fileName)
    {
        var address = _registry.Next();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(new Uri(address), $"api/files/{Uri.EscapeDataString(fileName)}"));
        var response = await _http.SendAsync(request);
        var instance = response.Headers.TryGetValues("X-Instance", out var values)
            ? values.FirstOrDefault()
            : null;
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, instance);
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsByteArrayAsync(), instance);
    }
}
