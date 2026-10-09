using Microsoft.AspNetCore.Mvc;
using ReportService.Dto;
using ReportService.Services.Interfaces;

namespace ReportService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReportItemDto>>> Get()
    {
        return await _reportService.GetReport();
    }
}
