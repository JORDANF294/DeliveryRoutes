namespace Backend;

/// <summary>
/// Validates starting position existence and directed reachability across required task locations.
/// </summary>
public static class RouteValidator
{
    public static void ValidateStartAndReachability(
        Graph<string> city,
        IReadOnlyList<Package> packages,
        string startLocation)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(packages);

        if (string.IsNullOrWhiteSpace(startLocation))
        {
            throw new ArgumentException("Starting location cannot be empty.", nameof(startLocation));
        }

        string normalizedStart = startLocation.Trim();

        if (!city.ContainsVertex(normalizedStart))
        {
            throw new InvalidOperationException(
                $"Starting location '{normalizedStart}' does not exist in the city map.");
        }

        HashSet<string> requiredLocations = new(StringComparer.Ordinal);
        foreach (Package package in packages)
        {
            requiredLocations.Add(package.Pickup);
            requiredLocations.Add(package.Delivery);
        }

        HashSet<string> reachableFromStart = GetReachableVertices(city, normalizedStart);
        foreach (string location in requiredLocations)
        {
            if (!reachableFromStart.Contains(location))
            {
                throw new InvalidOperationException(
                    $"Location '{location}' is not reachable from starting location '{normalizedStart}' following one-way streets.");
            }
        }

        foreach (string origin in requiredLocations)
        {
            HashSet<string> reachableFromOrigin = GetReachableVertices(city, origin);
            foreach (string target in requiredLocations)
            {
                if (!reachableFromOrigin.Contains(target))
                {
                    throw new InvalidOperationException(
                        $"Location '{target}' cannot be reached from '{origin}'. The courier could get trapped due to one-way streets.");
                }
            }
        }
    }

    private static HashSet<string> GetReachableVertices(Graph<string> city, string start)
    {
        HashSet<string> visited = new(StringComparer.Ordinal) { start };
        Queue<string> queue = new();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            foreach (Edge<string> edge in city.GetNeighbors(current))
            {
                if (visited.Add(edge.To))
                {
                    queue.Enqueue(edge.To);
                }
            }
        }

        return visited;
    }
}