using ManagerService.Dto;

namespace ManagerService.Services.Interfaces;

public interface IManagerService
{
    Task<List<ManagerResponseDto>> GetAll();
    Task<ManagerResponseDto?> GetById(int id);
    Task<ManagerResponseDto> Hire(CreateManagerDto dto);
    Task<ManagerResponseDto?> UpdateContracts(int id, UpdateContractsDto dto);
    Task<bool> Fire(int id);
    Task<List<ManagerResponseDto>> Report();
}
