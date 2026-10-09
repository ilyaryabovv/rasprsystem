using Microsoft.EntityFrameworkCore;
using ReportService.Data;
using ReportService.Dto;
using ReportService.Services.Interfaces;

namespace ReportService.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReportItemDto>> GetReport()
    {
        return await _db.Managers
            .OrderByDescending(m => m.ContractsCount)
            .Select(m => new ReportItemDto(m.Id, m.FullName, m.ContractsCount))
            .ToListAsync();
    }
}
