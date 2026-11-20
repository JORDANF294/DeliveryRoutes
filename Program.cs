using Backend;

try
{
    (string cityPath, string packagesPath, string startLocation) = ResolveInputs(args);

    Graph<string> city = CityFile.Load(cityPath);
    List<Package> packages = PackageFile.Load(packagesPath, city);
    RouteValidator.ValidateStartAndReachability(city, packages, startLocation);

    RoutePlanner planner = new(city, packages, startLocation);
    List<Stop> route = planner.Plan();

    if (!planner.IsValid(route))
    {
        throw new InvalidOperationException("Generated route failed validation.");
    }

    RoutePrinter.Print(startLocation, packages.Count, city, route, planner);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.ExitCode = 1;
}

static (string CityPath, string PackagesPath, string StartLocation) ResolveInputs(string[] args)
{
    if (args.Length == 3)
    {
        return (args[0].Trim(), args[1].Trim(), args[2].Trim());
    }

    if (args.Length != 0)
    {
        throw new ArgumentException(
            "Expected 0 arguments (interactive mode) or 3 arguments: <city.txt> <packages.txt> <startLocation>.");
    }

    Console.Write("City file path: ");
    string cityInput = Console.ReadLine()?.Trim() ?? string.Empty;

    Console.Write("Packages file path: ");
    string packagesInput = Console.ReadLine()?.Trim() ?? string.Empty;

    Console.Write("Starting location: ");
    string startInput = Console.ReadLine()?.Trim() ?? string.Empty;

    return (cityInput, packagesInput, startInput);
}
