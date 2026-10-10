using ManagerService.Clients;
using ManagerService.Dto;
using ManagerService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ManagerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;
    private readonly IReportClient _reportClient;

    public ManagerController(IManagerService managerService, IReportClient reportClient)
    {
        _managerService = managerService;
        _reportClient = reportClient;
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
}
