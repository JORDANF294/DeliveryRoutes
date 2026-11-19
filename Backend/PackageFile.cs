using System.Globalization;

namespace Backend;

/// <summary>
/// Loads, validates, and groups package pickup and delivery tasks from a flat file.
/// </summary>
public static class PackageFile
{
    private sealed class TaskDraft
    {
        public string? PickupLocation { get; set; }
        public int? PickupLine { get; set; }
        public string? DeliveryLocation { get; set; }
        public int? DeliveryLine { get; set; }
    }

    public static List<Package> Load(string path, Graph<string> city)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Package file path cannot be empty.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(city);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Package file not found: {path}", path);
        }

        Dictionary<int, TaskDraft> draftsById = new();
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
            if (parts.Length != 3)
            {
                throw new FormatException(
                    $"Line {lineNumber}: expected 3 comma-separated fields (location, action, package), got {parts.Length}.");
            }

            string location = parts[0].Trim();
            string actionRaw = parts[1].Trim();
            string packageIdRaw = parts[2].Trim();

            if (string.IsNullOrEmpty(location))
            {
                throw new FormatException(
                    $"Line {lineNumber}: location cannot be empty.");
            }

            if (!city.ContainsVertex(location))
            {
                throw new InvalidOperationException(
                    $"Line {lineNumber}: location '{location}' does not exist in the city map.");
            }

            string normalizedAction = actionRaw.ToLowerInvariant();
            if (normalizedAction is not ("pickup" or "delivery"))
            {
                throw new FormatException(
                    $"Line {lineNumber}: invalid action '{actionRaw}'. Expected 'Pickup' or 'Delivery'.");
            }

            if (!int.TryParse(
                    packageIdRaw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int packageId) ||
                packageId <= 0)
            {
                throw new FormatException(
                    $"Line {lineNumber}: package id '{packageIdRaw}' must be an integer greater than 0.");
            }

            if (!draftsById.TryGetValue(packageId, out TaskDraft? draft))
            {
                draft = new TaskDraft();
                draftsById[packageId] = draft;
            }

            if (normalizedAction == "pickup")
            {
                if (draft.PickupLocation is not null)
                {
                    throw new InvalidOperationException(
                        $"Line {lineNumber}: package {packageId} already has a Pickup task (first seen on line {draft.PickupLine}).");
                }

                draft.PickupLocation = location;
                draft.PickupLine = lineNumber;
            }
            else
            {
                if (draft.DeliveryLocation is not null)
                {
                    throw new InvalidOperationException(
                        $"Line {lineNumber}: package {packageId} already has a Delivery task (first seen on line {draft.DeliveryLine}).");
                }

                draft.DeliveryLocation = location;
                draft.DeliveryLine = lineNumber;
            }
        }

        if (draftsById.Count == 0)
        {
            throw new InvalidOperationException("Package file contains no valid package tasks.");
        }

        List<Package> packages = new(draftsById.Count);
        foreach ((int packageId, TaskDraft draft) in draftsById.OrderBy(entry => entry.Key))
        {
            if (draft.PickupLocation is null)
            {
                throw new InvalidOperationException(
                    $"Package {packageId} (delivery on line {draft.DeliveryLine}) is missing its Pickup task.");
            }

            if (draft.DeliveryLocation is null)
            {
                throw new InvalidOperationException(
                    $"Package {packageId} (pickup on line {draft.PickupLine}) is missing its Delivery task.");
            }

            packages.Add(new Package(packageId, draft.PickupLocation, draft.DeliveryLocation));
        }

        return packages;
    }
}