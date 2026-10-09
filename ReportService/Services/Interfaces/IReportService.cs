using ReportService.Dto;

namespace ReportService.Services.Interfaces;

public interface IReportService
{
    Task<List<ReportItemDto>> GetReport();
}
