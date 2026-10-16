using System.Text;
using ManagerService.Dto;

namespace ManagerService.Clients;

public static class ReportCsv
{
    public static byte[] Build(IReadOnlyList<ReportItemDto> items)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Id,FullName,ContractsCount");
        foreach (var item in items)
        {
            csv.Append(item.Id)
                .Append(',')
                .Append(Escape(item.FullName))
                .Append(',')
                .Append(item.ContractsCount)
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
