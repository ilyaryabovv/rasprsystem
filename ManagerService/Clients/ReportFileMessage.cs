namespace ManagerService.Clients;

public record ReportFileMessage(Guid SagaId, string FileName, string Content);
