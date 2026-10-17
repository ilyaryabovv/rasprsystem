using System.Net.Http.Headers;
using System.Net.Http.Json;
using ManagerService.Discovery;
using ManagerService.Dto;

namespace ManagerService.Clients;

public class FileClient : IFileClient
{
    private readonly HttpClient _http;
    private readonly FileInstanceRegistry _registry;
    private readonly ILogger<FileClient> _logger;

    public FileClient(HttpClient http, FileInstanceRegistry registry, ILogger<FileClient> logger)
    {
        _http = http;
        _registry = registry;
        _logger = logger;
    }

    public async Task<(StoredFileDto File, string? Instance)> Save(byte[] csv, string fileName)
    {
        var address = _registry.Next();
        _logger.LogInformation("Sending report file {FileName} to file service {Address}", fileName, address);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(csv);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(address), "api/files"));
        request.Content = content;
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var stored = (await response.Content.ReadFromJsonAsync<StoredFileDto>())!;
        return (stored, InstanceOf(response));
    }

    public async Task<(byte[]? Content, string? Instance)> Download(string fileName)
    {
        var address = _registry.Next();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(new Uri(address), $"api/files/{Uri.EscapeDataString(fileName)}"));
        var response = await _http.SendAsync(request);
        var instance = InstanceOf(response);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, instance);
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsByteArrayAsync(), instance);
    }

    private static string? InstanceOf(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("X-Instance", out var values)
            ? values.FirstOrDefault()
            : null;
    }
}
