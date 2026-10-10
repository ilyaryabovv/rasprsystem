using System.Net.Http.Json;
using ManagerService.Dto;

namespace ManagerService.Clients;

public class ReportClient : IReportClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ReportClient> _logger;

    public ReportClient(HttpClient http, ILogger<ReportClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ReportItemDto>> GetReport()
    {
        _logger.LogInformation("Calling report service");
        var report = await _http.GetFromJsonAsync<List<ReportItemDto>>("api/Report");
        return report ?? [];
    }
}
