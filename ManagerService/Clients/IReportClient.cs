using ManagerService.Dto;

namespace ManagerService.Clients;

public interface IReportClient
{
    Task<List<ReportItemDto>> GetReport();
}
