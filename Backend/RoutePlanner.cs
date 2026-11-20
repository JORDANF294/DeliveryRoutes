namespace Backend;

/// <summary>
/// Plans and evaluates pickup and delivery routes over a directed city graph.
/// </summary>
public class RoutePlanner
{
    private readonly Graph<string> _city;
    private readonly List<Package> _packages;
    private readonly Dictionary<int, Package> _packagesById;
    private readonly string _start;
    private readonly ShortestPaths _shortestPaths;

    public RoutePlanner(Graph<string> city, IReadOnlyList<Package> packages, string start)
    {
        _city = city ?? throw new ArgumentNullException(nameof(city));
        ArgumentNullException.ThrowIfNull(packages);

        if (string.IsNullOrWhiteSpace(start))
        {
            throw new ArgumentException("Starting location cannot be empty.", nameof(start));
        }

        _start = start.Trim();
        _packages = new List<Package>(packages);
        _packagesById = new Dictionary<int, Package>(_packages.Count);

        HashSet<string> relevantLocations = new(StringComparer.Ordinal) { _start };

        foreach (Package package in _packages)
        {
            if (!_packagesById.TryAdd(package.Id, package))
            {
                throw new ArgumentException($"Duplicate package id '{package.Id}'.", nameof(packages));
            }

            relevantLocations.Add(package.Pickup);
            relevantLocations.Add(package.Delivery);
        }

        _shortestPaths = new ShortestPaths(_city, relevantLocations);
    }

    public List<Stop> Plan()
    {
        return BuildGreedyRoute();
    }

    public double TotalDistance(List<Stop> route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (route.Count == 0)
        {
            return 0.0;
        }

        double totalMeters = 0.0;
        string currentLocation = _start;

        foreach (Stop stop in route)
        {
            totalMeters += _shortestPaths.GetDistance(currentLocation, stop.Location);
            currentLocation = stop.Location;
        }

        return totalMeters;
    }

    public bool IsValid(List<Stop> route)
    {
        if (route is null || route.Count != _packages.Count * 2)
        {
            return false;
        }

        HashSet<int> pickedUp = new();
        HashSet<int> delivered = new();

        foreach (Stop stop in route)
        {
            if (!_packagesById.TryGetValue(stop.PackageId, out Package package))
            {
                return false;
            }

            if (stop.Kind == StopKind.Pickup)
            {
                if (!string.Equals(stop.Location, package.Pickup, StringComparison.Ordinal))
                {
                    return false;
                }

                if (!pickedUp.Add(stop.PackageId))
                {
                    return false;
                }
            }
            else if (stop.Kind == StopKind.Delivery)
            {
                if (!pickedUp.Contains(stop.PackageId))
                {
                    return false;
                }

                if (!string.Equals(stop.Location, package.Delivery, StringComparison.Ordinal))
                {
                    return false;
                }

                if (!delivered.Add(stop.PackageId))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return pickedUp.Count == _packages.Count && delivered.Count == _packages.Count;
    }

    public List<string> PathBetween(string from, string to)
    {
        return _shortestPaths.GetPath(from, to);
    }

    internal List<Stop> BuildGreedyRoute()
    {
        List<Stop> route = new(_packages.Count * 2);
        HashSet<int> pickedUp = new();
        HashSet<int> delivered = new();
        string currentLocation = _start;

        int totalStops = _packages.Count * 2;
        while (route.Count < totalStops)
        {
            Stop? bestNextStop = null;
            double bestDistance = double.PositiveInfinity;

            foreach (Package package in _packages)
            {
                if (!pickedUp.Contains(package.Id))
                {
                    double distance = _shortestPaths.GetDistance(currentLocation, package.Pickup);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestNextStop = new Stop(package.Pickup, StopKind.Pickup, package.Id);
                    }
                }
                else if (!delivered.Contains(package.Id))
                {
                    double distance = _shortestPaths.GetDistance(currentLocation, package.Delivery);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestNextStop = new Stop(package.Delivery, StopKind.Delivery, package.Id);
                    }
                }
            }

            if (bestNextStop is null)
            {
                throw new InvalidOperationException("No valid next stop could be selected during greedy planning.");
            }

            Stop chosen = bestNextStop.Value;
            route.Add(chosen);
            currentLocation = chosen.Location;

            if (chosen.Kind == StopKind.Pickup)
            {
                pickedUp.Add(chosen.PackageId);
            }
            else
            {
                delivered.Add(chosen.PackageId);
            }
        }

        return route;
    }
}