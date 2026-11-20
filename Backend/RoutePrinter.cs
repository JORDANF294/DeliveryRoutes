using System.Globalization;
using System.Text;

namespace Backend;

/// <summary>
/// Formats and prints pickup and delivery routes in a human-readable step-by-step format.
/// </summary>
public static class RoutePrinter
{
    private sealed record StopGroup(
        string Location,
        List<Stop> Deliveries,
        List<Stop> Pickups);

    public static void Print(
        string start,
        int packageCount,
        int locationCount,
        IReadOnlyList<Stop> route,
        RoutePlanner planner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(start);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(planner);

        string normalizedStart = start.Trim();
        List<StopGroup> groups = GroupConsecutiveStops(route);

        StringBuilder output = new();
        output.AppendLine($"Posicion inicial: {normalizedStart}");
        output.AppendLine($"Paquetes: {packageCount} Ubicaciones: {locationCount}");

        string currentLocation = normalizedStart;
        double totalMeters = 0.0;
        int stopNumber = 1;

        foreach (StopGroup group in groups)
        {
            string actionsSummary = FormatActionsSummary(group.Deliveries, group.Pickups);
            bool isSameStartingPoint =
                stopNumber == 1 &&
                string.Equals(currentLocation, group.Location, StringComparison.Ordinal) &&
                Math.Abs(totalMeters) < 1e-9;

            if (isSameStartingPoint)
            {
                output.AppendLine($"{stopNumber}. En {group.Location}: {actionsSummary}.");
            }
            else
            {
                List<string> path = planner.PathBetween(currentLocation, group.Location);
                double segmentMeters = ComputePathDistance(path, planner);
                totalMeters += segmentMeters;

                int roundedMeters = (int)Math.Round(segmentMeters, MidpointRounding.AwayFromZero);
                output.AppendLine(
                    $"{stopNumber}. Ir de {currentLocation} a {group.Location} ({roundedMeters} m): {actionsSummary}.");
                output.AppendLine($"Camino: {string.Join(" -> ", path)}");
            }

            currentLocation = group.Location;
            stopNumber++;
        }

        double totalKilometers = totalMeters / 1000.0;
        output.AppendLine("Todos los paquetes fueron recogidos y entregados.");
        output.AppendLine(
            $"Recorrido total: {totalKilometers.ToString("0.00", CultureInfo.InvariantCulture)} km");

        Console.Write(output.ToString());
    }

    private static List<StopGroup> GroupConsecutiveStops(IReadOnlyList<Stop> route)
    {
        List<StopGroup> groups = new();

        foreach (Stop stop in route)
        {
            if (groups.Count > 0 &&
                string.Equals(groups[^1].Location, stop.Location, StringComparison.Ordinal))
            {
                if (stop.Kind == StopKind.Delivery)
                {
                    groups[^1].Deliveries.Add(stop);
                }
                else
                {
                    groups[^1].Pickups.Add(stop);
                }
            }
            else
            {
                List<Stop> deliveries = new();
                List<Stop> pickups = new();

                if (stop.Kind == StopKind.Delivery)
                {
                    deliveries.Add(stop);
                }
                else
                {
                    pickups.Add(stop);
                }

                groups.Add(new StopGroup(stop.Location, deliveries, pickups));
            }
        }

        foreach (StopGroup group in groups)
        {
            group.Deliveries.Sort((a, b) => a.PackageId.CompareTo(b.PackageId));
            group.Pickups.Sort((a, b) => a.PackageId.CompareTo(b.PackageId));
        }

        return groups;
    }

    private static string FormatActionsSummary(List<Stop> deliveries, List<Stop> pickups)
    {
        List<string> clauses = new(2);

        if (deliveries.Count > 0)
        {
            string label = deliveries.Count == 1 ? "entregar paquete" : "entregar paquetes";
            clauses.Add($"{label} {FormatPackageIds(deliveries)}");
        }

        if (pickups.Count > 0)
        {
            string label = pickups.Count == 1 ? "recoger paquete" : "recoger paquetes";
            clauses.Add($"{label} {FormatPackageIds(pickups)}");
        }

        return string.Join(" y ", clauses);
    }

    private static string FormatPackageIds(List<Stop> stops)
    {
        List<int> ids = stops.Select(s => s.PackageId).ToList();
        if (ids.Count == 1)
        {
            return ids[0].ToString(CultureInfo.InvariantCulture);
        }

        if (ids.Count == 2)
        {
            return $"{ids[0]} y {ids[1]}";
        }

        string head = string.Join(", ", ids.Take(ids.Count - 1));
        return $"{head} y {ids[^1]}";
    }

    private static double ComputePathDistance(IReadOnlyList<string> path, RoutePlanner planner)
    {
        if (path.Count <= 1)
        {
            return 0.0;
        }

        double meters = 0.0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            meters += planner.DistanceBetween(path[i], path[i + 1]);
        }

        return meters;
    }
}