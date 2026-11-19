namespace Backend;

/// <summary>
/// Represents whether a route stop is a package pickup or delivery.
/// </summary>
public enum StopKind
{
    Pickup,
    Delivery
}

/// <summary>
/// Represents an individual task stop at a city location for a specific package.
/// </summary>
public readonly record struct Stop(string Location, StopKind Kind, int PackageId);