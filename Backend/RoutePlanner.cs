namespace Backend;

/// <summary>
/// Plans and evaluates pickup and delivery routes over a directed city graph.
/// </summary>
public class RoutePlanner
{
    private const int DefaultSeed = 42;
    private const int RandomRestarts = 35;

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
        List<Stop> bestRoute = ImproveRoute(BuildGreedyRoute());
        double bestDistance = TotalDistance(bestRoute);

        List<Stop> insertionFromDefaultOrder = ImproveRoute(BuildCheapestInsertionRoute(_packages));
        double insertionDefaultDistance = TotalDistance(insertionFromDefaultOrder);
        if (insertionDefaultDistance < bestDistance && IsValid(insertionFromDefaultOrder))
        {
            bestRoute = insertionFromDefaultOrder;
            bestDistance = insertionDefaultDistance;
        }

        Random rng = new(DefaultSeed);
        List<Package> shuffledPackages = new(_packages);

        for (int restart = 0; restart < RandomRestarts; restart++)
        {
            Shuffle(shuffledPackages, rng);
            List<Stop> candidate = ImproveRoute(BuildCheapestInsertionRoute(shuffledPackages));
            double candidateDistance = TotalDistance(candidate);

            if (candidateDistance < bestDistance && IsValid(candidate))
            {
                bestRoute = candidate;
                bestDistance = candidateDistance;
            }
        }

        return bestRoute;
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

    public double DistanceBetween(string from, string to)
    {
        return _shortestPaths.GetDistance(from, to);
    }

    public double StepDistance(string from, string to)
    {
        return _shortestPaths.GetStepDistance(from, to);
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

    private List<Stop> BuildCheapestInsertionRoute(IReadOnlyList<Package> orderedPackages)
    {
        List<Stop> route = new(orderedPackages.Count * 2);

        foreach (Package package in orderedPackages)
        {
            Stop pickup = new(package.Pickup, StopKind.Pickup, package.Id);
            Stop delivery = new(package.Delivery, StopKind.Delivery, package.Id);

            int bestPickupIndex = 0;
            int bestDeliveryIndex = 1;
            double bestCost = double.PositiveInfinity;

            for (int pIdx = 0; pIdx <= route.Count; pIdx++)
            {
                for (int dIdx = pIdx; dIdx <= route.Count; dIdx++)
                {
                    List<Stop> candidate = new(route.Count + 2);
                    candidate.AddRange(route.Take(pIdx));
                    candidate.Add(pickup);
                    candidate.AddRange(route.Skip(pIdx).Take(dIdx - pIdx));
                    candidate.Add(delivery);
                    candidate.AddRange(route.Skip(dIdx));

                    double cost = TotalDistance(candidate);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestPickupIndex = pIdx;
                        bestDeliveryIndex = dIdx;
                    }
                }
            }

            List<Stop> updated = new(route.Count + 2);
            updated.AddRange(route.Take(bestPickupIndex));
            updated.Add(pickup);
            updated.AddRange(route.Skip(bestPickupIndex).Take(bestDeliveryIndex - bestPickupIndex));
            updated.Add(delivery);
            updated.AddRange(route.Skip(bestDeliveryIndex));
            route = updated;
        }

        return route;
    }

    private List<Stop> ImproveRoute(List<Stop> initialRoute)
    {
        List<Stop> current = new(initialRoute);
        double currentDistance = TotalDistance(current);

        bool improved = true;
        while (improved)
        {
            improved = false;

            // 1. Relocate a package pair (pickup + delivery) to its best valid positions.
            foreach (Package package in _packages)
            {
                int pPos = current.FindIndex(s => s.PackageId == package.Id && s.Kind == StopKind.Pickup);
                int dPos = current.FindIndex(s => s.PackageId == package.Id && s.Kind == StopKind.Delivery);
                if (pPos < 0 || dPos < 0)
                {
                    continue;
                }

                List<Stop> withoutPair = new(current.Count - 2);
                for (int i = 0; i < current.Count; i++)
                {
                    if (i != pPos && i != dPos)
                    {
                        withoutPair.Add(current[i]);
                    }
                }

                Stop pickup = current[pPos];
                Stop delivery = current[dPos];

                List<Stop>? bestCandidate = null;
                double bestDistance = currentDistance - 1e-6;

                for (int i = 0; i <= withoutPair.Count; i++)
                {
                    for (int j = i; j <= withoutPair.Count; j++)
                    {
                        List<Stop> candidate = new(withoutPair.Count + 2);
                        candidate.AddRange(withoutPair.Take(i));
                        candidate.Add(pickup);
                        candidate.AddRange(withoutPair.Skip(i).Take(j - i));
                        candidate.Add(delivery);
                        candidate.AddRange(withoutPair.Skip(j));

                        double dist = TotalDistance(candidate);
                        if (dist < bestDistance && IsValid(candidate))
                        {
                            bestDistance = dist;
                            bestCandidate = candidate;
                        }
                    }
                }

                if (bestCandidate is not null)
                {
                    current = bestCandidate;
                    currentDistance = bestDistance;
                    improved = true;
                }
            }

            // 2. Relocate a single stop while keeping pickup strictly before delivery.
            for (int fromIdx = 0; fromIdx < current.Count; fromIdx++)
            {
                Stop movingStop = current[fromIdx];
                List<Stop> withoutStop = new(current.Count - 1);
                for (int i = 0; i < current.Count; i++)
                {
                    if (i != fromIdx)
                    {
                        withoutStop.Add(current[i]);
                    }
                }

                List<Stop>? bestCandidate = null;
                double bestDistance = currentDistance - 1e-6;

                for (int toIdx = 0; toIdx <= withoutStop.Count; toIdx++)
                {
                    List<Stop> candidate = new(withoutStop.Count + 1);
                    candidate.AddRange(withoutStop.Take(toIdx));
                    candidate.Add(movingStop);
                    candidate.AddRange(withoutStop.Skip(toIdx));

                    if (!IsValid(candidate))
                    {
                        continue;
                    }

                    double dist = TotalDistance(candidate);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate is not null)
                {
                    current = bestCandidate;
                    currentDistance = bestDistance;
                    improved = true;
                }
            }

            // 3. Swap pairs of stops when valid and beneficial.
            for (int i = 0; i < current.Count - 1; i++)
            {
                for (int j = i + 1; j < current.Count; j++)
                {
                    List<Stop> candidate = new(current);
                    (candidate[i], candidate[j]) = (candidate[j], candidate[i]);

                    if (!IsValid(candidate))
                    {
                        continue;
                    }

                    double dist = TotalDistance(candidate);
                    if (dist < currentDistance - 1e-6)
                    {
                        current = candidate;
                        currentDistance = dist;
                        improved = true;
                    }
                }
            }
        }

        return current;
    }

    private static void Shuffle<T>(IList<T> items, Random rng)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}