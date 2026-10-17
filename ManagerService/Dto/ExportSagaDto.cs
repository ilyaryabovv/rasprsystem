namespace ManagerService.Dto;

public record ExportSagaDto(Guid SagaId, string FileName, string Status, string? Instance);
