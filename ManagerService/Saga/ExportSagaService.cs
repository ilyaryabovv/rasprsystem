using ManagerService.Data;
using ManagerService.Models;

namespace ManagerService.Saga;

public class ExportSagaService
{
    private readonly AppDbContext _db;

    public ExportSagaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ExportSaga> Start(string fileName)
    {
        var saga = new ExportSaga
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            Status = "Pending"
        };
        _db.ExportSagas.Add(saga);
        await _db.SaveChangesAsync();
        return saga;
    }

    public Task<ExportSaga?> Find(Guid id)
    {
        return _db.ExportSagas.FindAsync(id).AsTask();
    }

    public async Task<bool> Apply(Guid id, string status, string? instance)
    {
        var saga = await _db.ExportSagas.FindAsync(id);
        if (saga is null)
        {
            return false;
        }

        if (saga.Status is "Completed" or "Failed")
        {
            return true;
        }

        saga.Status = status;
        saga.Instance = instance;
        await _db.SaveChangesAsync();
        return true;
    }
}
