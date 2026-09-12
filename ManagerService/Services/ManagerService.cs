using ManagerService.Data;
using ManagerService.Dto;
using ManagerService.Models;
using ManagerService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ManagerService.Services;

public class ManagerService : IManagerService
{
    private readonly AppDbContext _db;

    public ManagerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ManagerResponseDto>> GetAll()
    {
        var managers = await _db.Managers.OrderBy(m => m.Id).ToListAsync();
        return managers.Select(ToDto).ToList();
    }

    public async Task<ManagerResponseDto?> GetById(int id)
    {
        var manager = await _db.Managers.FindAsync(id);
        return manager is null ? null : ToDto(manager);
    }

    public async Task<ManagerResponseDto> Hire(CreateManagerDto dto)
    {
        var manager = new Manager
        {
            FullName = dto.FullName.Trim(),
            ContractsCount = 0
        };

        _db.Managers.Add(manager);
        await _db.SaveChangesAsync();
        return ToDto(manager);
    }

    public async Task<ManagerResponseDto?> UpdateContracts(int id, UpdateContractsDto dto)
    {
        var manager = await _db.Managers.FindAsync(id);
        if (manager is null)
        {
            return null;
        }

        manager.ContractsCount = dto.ContractsCount;
        await _db.SaveChangesAsync();
        return ToDto(manager);
    }

    public async Task<bool> Fire(int id)
    {
        var manager = await _db.Managers.FindAsync(id);
        if (manager is null)
        {
            return false;
        }

        _db.Managers.Remove(manager);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ManagerResponseDto>> Report()
    {
        var managers = await _db.Managers
            .OrderByDescending(m => m.ContractsCount)
            .ToListAsync();

        return managers.Select(ToDto).ToList();
    }

    private static ManagerResponseDto ToDto(Manager manager) =>
        new(manager.Id, manager.FullName, manager.ContractsCount);
}
