using System.Globalization;

namespace Backend;

/// <summary>
/// Loads and validates a city map flat file into a directed weighted graph.
/// </summary>
public static class CityFile
{
    public static Graph<string> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("City file path cannot be empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"City file not found: {path}", path);
        }

        Graph<string> graph = new(directed: true);
        HashSet<(string A, string B)> seenStreets = new();

        string[] lines = File.ReadAllLines(path);

        for (int index = 0; index < lines.Length; index++)
        {
            int lineNumber = index + 1;
            string trimmed = lines[index].Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            string[] parts = trimmed.Split(',');
            if (parts.Length != 4)
            {
                throw new FormatException(
                    $"Line {lineNumber}: expected 4 comma-separated fields (from, to, distance_in_meters, direction), got {parts.Length}.");
            }

            string from = parts[0].Trim();
            string to = parts[1].Trim();
            string distanceRaw = parts[2].Trim();
            string directionRaw = parts[3].Trim();

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                throw new FormatException(
                    $"Line {lineNumber}: origin and destination locations cannot be empty.");
            }

            if (!double.TryParse(
                    distanceRaw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double distance) ||
                double.IsNaN(distance) ||
                double.IsInfinity(distance) ||
                distance <= 0)
            {
                throw new FormatException(
                    $"Line {lineNumber}: distance '{distanceRaw}' must be a positive number greater than 0.");
            }

            string normalizedDirection = directionRaw.ToLowerInvariant();
            if (normalizedDirection is not ("twoway" or "oneway"))
            {
                throw new FormatException(
                    $"Line {lineNumber}: invalid direction '{directionRaw}'. Expected 'Twoway' or 'Oneway'.");
            }

            (string A, string B) streetKey = NormalizeStreetPair(from, to);
            if (!seenStreets.Add(streetKey))
            {
                throw new InvalidOperationException(
                    $"Line {lineNumber}: duplicate street between '{from}' and '{to}'.");
            }

            if (normalizedDirection == "oneway")
            {
                graph.AddEdge(from, to, distance);
            }
            else
            {
                graph.AddEdge(from, to, distance);
                graph.AddEdge(to, from, distance);
            }
        }

        return graph;
    }

    private static (string A, string B) NormalizeStreetPair(string first, string second)
    {
        return string.CompareOrdinal(first, second) <= 0
            ? (first, second)
            : (second, first);
    }
}