namespace Omadeas.Classifier.Infrastructure.Caching.Helpers;

/// <summary>
/// Helper class for building consistent cache keys with service isolation.
/// </summary>
public static class CacheKeyBuilder
{
    /// <summary>
    /// The service name used as a prefix for all cache keys.
    /// </summary>
    private const string ServicePrefix = "classifier";

    /// <summary>
    /// Builds a cache key with service prefix and parameters.
    /// </summary>
    /// <param name="resource">The resource type (e.g., 'classifier', 'profile')</param>
    /// <param name="parameters">Additional parameters for the key</param>
    /// <returns>A formatted cache key with service isolation</returns>
    public static string Build(string resource, params object?[] parameters)
    {
        var keyParts = new List<string> { ServicePrefix, resource };

        foreach (var param in parameters)
        {
            keyParts.Add(param?.ToString() ?? "null");
        }

        return string.Join(":", keyParts);
    }

    /// <summary>
    /// Builds a cache key for a specific entity by ID.
    /// </summary>
    /// <param name="resource">The resource type</param>
    /// <param name="id">The entity ID</param>
    /// <returns>A formatted cache key</returns>
    public static string BuildById(string resource, Guid id)
    {
        return Build(resource, "id", id);
    }

    /// <summary>
    /// Builds a cache key for a specific entity by string ID.
    /// </summary>
    /// <param name="resource">The resource type</param>
    /// <param name="id">The entity ID</param>
    /// <returns>A formatted cache key</returns>
    public static string BuildById(string resource, string id)
    {
        return Build(resource, "id", id);
    }

    /// <summary>
    /// Builds a cache key for a list/collection with filters.
    /// </summary>
    /// <param name="resource">The resource type</param>
    /// <param name="filters">Filter parameters</param>
    /// <returns>A formatted cache key</returns>
    public static string BuildForList(string resource, params object?[] filters)
    {
        return Build(resource, "list", string.Join(":", filters.Select(f => f?.ToString() ?? "null")));
    }

    /// <summary>
    /// Builds a cache key for paginated results.
    /// </summary>
    /// <param name="resource">The resource type</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="filters">Additional filter parameters</param>
    /// <returns>A formatted cache key</returns>
    public static string BuildForPage(string resource, int pageNumber, int pageSize, params object?[] filters)
    {
        var filterString = filters.Length > 0 ? string.Join(":", filters.Select(f => f?.ToString() ?? "null")) : "nofilter";
        return Build(resource, "page", pageNumber, pageSize, filterString);
    }
}
