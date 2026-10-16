using System.Net.Http.Headers;
using System.Net.Http.Json;
using ManagerService.Dto;

namespace ManagerService.Clients;

public class FileClient : IFileClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FileClient> _logger;

    public FileClient(HttpClient http, ILogger<FileClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<StoredFileDto> Save(byte[] csv, string fileName)
    {
        _logger.LogInformation("Sending report file {FileName} to file service", fileName);
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(csv);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", fileName);

        var response = await _http.PostAsync("api/files", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StoredFileDto>())!;
    }

    public async Task<byte[]?> Download(string fileName)
    {
        var response = await _http.GetAsync($"api/files/{Uri.EscapeDataString(fileName)}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
