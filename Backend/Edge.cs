namespace Backend;

/// <summary>
/// Represents a directed, weighted edge from a source vertex to a target vertex.
/// </summary>
public sealed record Edge<T>(T To, double Weight) where T : notnull;