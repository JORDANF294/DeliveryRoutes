namespace Backend;

/// <summary>
/// Represents an adjacency-list graph that can operate as directed or undirected.
/// </summary>
public class Graph<T> where T : notnull
{
    private readonly Dictionary<T, List<Edge<T>>> _adjacency = new();

    public bool IsDirected { get; }

    public int VertexCount => _adjacency.Count;

    public IEnumerable<T> Vertices => _adjacency.Keys;

    public Graph(bool directed = true)
    {
        IsDirected = directed;
    }

    public bool ContainsVertex(T vertex) => _adjacency.ContainsKey(vertex);

    public bool AddVertex(T vertex)
    {
        if (_adjacency.ContainsKey(vertex))
        {
            return false;
        }

        _adjacency[vertex] = new List<Edge<T>>();
        return true;
    }

    public void AddEdge(T from, T to, double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Edge weight must be positive.");
        }

        AddVertex(from);
        AddVertex(to);

        _adjacency[from].Add(new Edge<T>(to, weight));

        if (!IsDirected)
        {
            _adjacency[to].Add(new Edge<T>(from, weight));
        }
    }

    public IReadOnlyList<Edge<T>> GetNeighbors(T vertex)
    {
        if (!_adjacency.TryGetValue(vertex, out List<Edge<T>>? edges))
        {
            return Array.Empty<Edge<T>>();
        }

        return edges;
    }

    public bool HasEdge(T from, T to)
    {
        if (!_adjacency.TryGetValue(from, out List<Edge<T>>? edges))
        {
            return false;
        }

        return edges.Any(edge => EqualityComparer<T>.Default.Equals(edge.To, to));
    }
}