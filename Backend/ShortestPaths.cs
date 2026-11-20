namespace Backend;

/// <summary>
/// Precomputes shortest paths and street sequences between locations using Dijkstra's algorithm.
/// </summary>
public class ShortestPaths
{
    private readonly Graph<string> _city;
    private readonly Dictionary<string, Dictionary<string, double>> _distances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string?>> _previous = new(StringComparer.Ordinal);

    public ShortestPaths(Graph<string> city, IEnumerable<string> relevantLocations)
    {
        _city = city ?? throw new ArgumentNullException(nameof(city));
        ArgumentNullException.ThrowIfNull(relevantLocations);

        HashSet<string> origins = new(_city.Vertices, StringComparer.Ordinal);
        foreach (string location in relevantLocations)
        {
            if (!_city.ContainsVertex(location))
            {
                throw new ArgumentException($"Vertex '{location}' does not exist in the graph.", nameof(relevantLocations));
            }

            origins.Add(location);
        }

        foreach (string origin in origins)
        {
            (Dictionary<string, double> dist, Dictionary<string, string?> prev) = RunDijkstra(_city, origin);
            _distances[origin] = dist;
            _previous[origin] = prev;
        }
    }

    public double GetDistance(string from, string to)
    {
        if (!_distances.TryGetValue(from, out Dictionary<string, double>? fromDistances) ||
            !fromDistances.TryGetValue(to, out double distance) ||
            double.IsPositiveInfinity(distance))
        {
            throw new InvalidOperationException($"No directed path exists from '{from}' to '{to}'.");
        }

        return distance;
    }

    public double GetStepDistance(string from, string to)
    {
        double minWeight = double.PositiveInfinity;
        foreach (Edge<string> edge in _city.GetNeighbors(from))
        {
            if (string.Equals(edge.To, to, StringComparison.Ordinal) && edge.Weight < minWeight)
            {
                minWeight = edge.Weight;
            }
        }

        if (!double.IsPositiveInfinity(minWeight))
        {
            return minWeight;
        }

        return GetDistance(from, to);
    }

    public List<string> GetPath(string from, string to)
    {
        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return new List<string> { from };
        }

        _ = GetDistance(from, to);

        Dictionary<string, string?> prevMap = _previous[from];
        List<string> path = new();
        string? current = to;

        while (current is not null)
        {
            path.Add(current);
            if (string.Equals(current, from, StringComparison.Ordinal))
            {
                break;
            }

            current = prevMap.TryGetValue(current, out string? predecessor) ? predecessor : null;
        }

        path.Reverse();

        if (path.Count == 0 || !string.Equals(path[0], from, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Failed to reconstruct path from '{from}' to '{to}'.");
        }

        return path;
    }

    private static (Dictionary<string, double> Distances, Dictionary<string, string?> Previous) RunDijkstra(
        Graph<string> city,
        string source)
    {
        Dictionary<string, double> distances = new(StringComparer.Ordinal);
        Dictionary<string, string?> previous = new(StringComparer.Ordinal);
        HashSet<string> settled = new(StringComparer.Ordinal);

        foreach (string vertex in city.Vertices)
        {
            distances[vertex] = double.PositiveInfinity;
            previous[vertex] = null;
        }

        distances[source] = 0.0;

        PriorityQueue<string, double> queue = new();
        queue.Enqueue(source, 0.0);

        while (queue.Count > 0)
        {
            queue.TryDequeue(out string? current, out double currentDistance);
            if (current is null || !settled.Add(current))
            {
                continue;
            }

            if (currentDistance > distances[current])
            {
                continue;
            }

            foreach (Edge<string> edge in city.GetNeighbors(current))
            {
                if (settled.Contains(edge.To))
                {
                    continue;
                }

                double candidateDistance = distances[current] + edge.Weight;
                if (candidateDistance < distances[edge.To])
                {
                    distances[edge.To] = candidateDistance;
                    previous[edge.To] = current;
                    queue.Enqueue(edge.To, candidateDistance);
                }
            }
        }

        return (distances, previous);
    }
}