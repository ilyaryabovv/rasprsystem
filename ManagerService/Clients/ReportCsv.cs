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

    public static List<ReportItemDto> Parse(byte[] csv)
    {
        var text = Encoding.UTF8.GetString(csv).TrimStart('\uFEFF');
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var items = new List<ReportItemDto>();
        foreach (var line in lines.Skip(1))
        {
            var columns = Split(line);
            if (columns.Count < 3
                || !int.TryParse(columns[0], out var id)
                || !int.TryParse(columns[2], out var contracts))
            {
                continue;
            }

            items.Add(new ReportItemDto(id, columns[1], contracts));
        }

        return items;
    }

    private static List<string> Split(string line)
    {
        var columns = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (quoted && character == '"' && i + 1 < line.Length && line[i + 1] == '"')
            {
                value.Append('"');
                i++;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                columns.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        columns.Add(value.ToString());
        return columns;
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
