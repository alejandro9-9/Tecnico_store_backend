using System.Text;

namespace Evol.Infrastructure;

internal static class LectorCsv
{
    public static List<Dictionary<string, string>> Read(string path)
    {
        if (!File.Exists(path)) return [];
        using var reader = new StreamReader(path, Encoding.UTF8, true);
        var header = Parse(reader.ReadLine() ?? "").Select(field => field.Trim()).ToArray();
        var rows = new List<Dictionary<string, string>>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = Parse(line);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < header.Length; index++) row[header[index]] = index < values.Count ? values[index].Trim() : "";
            rows.Add(row);
        }
        return rows;
    }

    private static List<string> Parse(string line)
    {
        var fields = new List<string>(); var current = new StringBuilder(); var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"') { current.Append('"'); index++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(character);
        }
        fields.Add(current.ToString());
        return fields;
    }
}




