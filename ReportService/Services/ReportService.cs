using Microsoft.EntityFrameworkCore;
using ReportService.Data;
using ReportService.Dto;
using ReportService.Services.Interfaces;

namespace ReportService.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(AppDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ReportItemDto>> GetReport()
    {
        var report = await _db.Managers
            .OrderByDescending(m => m.ContractsCount)
            .Select(m => new ReportItemDto(m.Id, m.FullName, m.ContractsCount))
            .ToListAsync();

        _logger.LogInformation("Report formed with {Count} managers", report.Count);
        return report;
    }
}
