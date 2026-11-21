using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace ReportService.Logging;

public class TraceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (string.IsNullOrEmpty(traceId))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
    }
}
