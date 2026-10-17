namespace ManagerService.Clients;

public interface IReportPublisher
{
    Task Publish(Guid sagaId, byte[] csv, string fileName);
}
