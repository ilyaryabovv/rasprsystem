using ManagerService.Dto;

namespace ManagerService.Clients;

public record ReportFromFile(List<ReportItemDto>? Items, string? Instance);

public static class ReportFileFuture
{
    public static Task<ReportFromFile> Start(Task<(byte[]? Content, string? Instance)> download)
    {
        return download.ContinueWith(task =>
        {
            var (content, instance) = task.GetAwaiter().GetResult();
            var items = content is null ? null : ReportCsv.Parse(content);
            return new ReportFromFile(items, instance);
        }, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
    }
}
