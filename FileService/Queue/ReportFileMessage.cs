namespace FileService.Queue;

public record ReportFileMessage(Guid SagaId, string FileName, string Content);
