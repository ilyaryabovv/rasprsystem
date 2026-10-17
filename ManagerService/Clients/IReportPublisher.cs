namespace ManagerService.Clients;

public interface IReportPublisher
{
    Task Publish(byte[] csv, string fileName);
}
