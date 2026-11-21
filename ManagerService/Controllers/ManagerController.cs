using ManagerService.Clients;
using ManagerService.Discovery;
using ManagerService.Dto;
using ManagerService.Models;
using ManagerService.Saga;
using ManagerService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagerService.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;
    private readonly IReportClient _reportClient;
    private readonly IReportPublisher _publisher;
    private readonly IFileClient _fileClient;
    private readonly ExportSagaService _sagas;
    private readonly FileInstanceRegistry _fileInstances;

    public ManagerController(
        IManagerService managerService,
        IReportClient reportClient,
        IReportPublisher publisher,
        IFileClient fileClient,
        ExportSagaService sagas,
        FileInstanceRegistry fileInstances)
    {
        _managerService = managerService;
        _reportClient = reportClient;
        _publisher = publisher;
        _fileClient = fileClient;
        _sagas = sagas;
        _fileInstances = fileInstances;
    }

    [HttpGet]
    public async Task<ActionResult<List<ManagerResponseDto>>> GetAll()
    {
        return await _managerService.GetAll();
    }

    [HttpGet("report")]
    public async Task<ActionResult<List<ReportItemDto>>> Report()
    {
        try
        {
            return await _reportClient.GetReport();
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost("report/export")]
    public async Task<ActionResult<ExportSagaDto>> ExportReport()
    {
        List<ReportItemDto> report;
        try
        {
            report = await _reportClient.GetReport();
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        var fileName = $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        var saga = await _sagas.Start(fileName);
        try
        {
            await _publisher.Publish(saga.Id, ReportCsv.Build(report), fileName);
        }
        catch (Exception)
        {
            await _sagas.Apply(saga.Id, "Failed", null);
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        return ToDto(saga);
    }

    [HttpGet("report/file/{name}")]
    public async Task<ActionResult<List<ReportItemDto>>> ReportFromStorage(string name)
    {
        try
        {
            var future = ReportFileFuture.Start(_fileClient.Download(name));
            var report = await future;
            if (!string.IsNullOrWhiteSpace(report.Instance))
            {
                Response.Headers["X-File-Instance"] = report.Instance;
            }

            return report.Items is null ? NotFound() : report.Items;
        }
        catch (Exception exception) when (exception is HttpRequestException or AggregateException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("report/export/saga/{id:guid}")]
    public async Task<ActionResult<ExportSagaDto>> GetSaga(Guid id)
    {
        var saga = await _sagas.Find(id);
        return saga is null ? NotFound() : ToDto(saga);
    }

    [HttpGet("report/export/{name}")]
    public async Task<IActionResult> DownloadReport(string name)
    {
        try
        {
            var (content, instance) = await _fileClient.Download(name);
            if (!string.IsNullOrWhiteSpace(instance))
            {
                Response.Headers["X-File-Instance"] = instance;
            }

            return content is null ? NotFound() : File(content, "text/csv", name);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("files/destinations")]
    public ActionResult<IReadOnlyList<string>> FileDestinations()
    {
        return Ok(_fileInstances.Addresses);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ManagerResponseDto>> GetById(int id)
    {
        var manager = await _managerService.GetById(id);
        return manager is null ? NotFound() : manager;
    }

    [HttpPost]
    public async Task<ActionResult<ManagerResponseDto>> Hire([FromBody] CreateManagerDto dto)
    {
        var manager = await _managerService.Hire(dto);
        return CreatedAtAction(nameof(GetById), new { id = manager.Id }, manager);
    }

    [HttpPut("{id:int}/contracts")]
    public async Task<ActionResult<ManagerResponseDto>> UpdateContracts(int id, [FromBody] UpdateContractsDto dto)
    {
        var manager = await _managerService.UpdateContracts(id, dto);
        return manager is null ? NotFound() : manager;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Fire(int id)
    {
        var deleted = await _managerService.Fire(id);
        return deleted ? NoContent() : NotFound();
    }

    private static ExportSagaDto ToDto(ExportSaga saga)
    {
        return new ExportSagaDto(saga.Id, saga.FileName, saga.Status, saga.Instance);
    }
}
