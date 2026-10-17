namespace ManagerService.Saga;

public record ReportSagaResult(Guid SagaId, string FileName, string Status, string? Instance);
