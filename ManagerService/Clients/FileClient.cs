namespace ManagerService.Clients;

public class FileClient : IFileClient
{
    private readonly HttpClient _http;

    public FileClient(HttpClient http)
    {
        _http = http;
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
